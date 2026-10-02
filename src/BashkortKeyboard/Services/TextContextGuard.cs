using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Automation;
using System.Windows.Automation.Text;
using BashkortKeyboard.Interop;

namespace BashkortKeyboard.Services;

// UI Automation providers are external code and may block. They run exclusively
// on this background MTA thread, never on the keyboard hook or settings UI.
internal sealed class TextContextGuard : IDisposable
{
    private sealed record SafeFocus(InputContext Context, int[] RuntimeId, long Time);
    private sealed record Anchor(int[] RuntimeId, TextPatternRange Caret, nint NativeCaret, NativeMethods.Rect Rect);
    private readonly BlockingCollection<Action> work = new(16);
    private readonly ConcurrentDictionary<Guid, byte> alive = new();
    private readonly Dictionary<Guid, Anchor> anchors = new();
    private readonly KeyboardLayoutService layouts = new();
    private readonly Thread worker;
    private SafeFocus? safe;
    private volatile bool stopping;
    private long lastProbe;

    public TextContextGuard()
    {
        worker = new Thread(Loop) { IsBackground = true, Name = "Text safety / UI Automation" };
        worker.SetApartmentState(ApartmentState.MTA);
        worker.Start();
    }
    public bool IsEligible(InputContext context)
    {
        var cached = Volatile.Read(ref safe);
        return cached is not null && cached.Context == context &&
            Stopwatch.GetElapsedTime(cached.Time).TotalMilliseconds < 300;
    }
    public void InvalidateFocus() => Volatile.Write(ref safe, null);
    private bool Queue(Action action)
    {
        try { return !stopping && work.TryAdd(action); }
        catch (InvalidOperationException) { return false; }
    }
    public void Capture(Press press)
    {
        alive.TryAdd(press.Id, 0);
        Queue(() =>
        {
            if (!alive.ContainsKey(press.Id)) return;
            try
            {
                var cached = Volatile.Read(ref safe);
                if (cached is null || cached.Context != press.Context || layouts.Current() != press.Context) return;
                var element = EditableFocusedElement();
                if (element is null || !element.GetRuntimeId().SequenceEqual(cached.RuntimeId)) return;
                var caret = Caret(element);
                if (caret is null || !PreviousIs(caret, press.Expected)) return;
                var native = NativeCaret();
                if (layouts.Current() != press.Context || !alive.ContainsKey(press.Id)) return;
                anchors[press.Id] = new(element.GetRuntimeId(), caret.Clone(), native.Caret, native.CaretRect);
            }
            catch { /* Unknown provider => fail closed, without exception text. */ }
        });
    }
    public void Validate(Press press, Action<bool> completed)
    {
        if (!Queue(() =>
        {
            bool result = false;
            try
            {
                if (alive.ContainsKey(press.Id) && anchors.TryGetValue(press.Id, out var anchor) &&
                    layouts.Current() == press.Context && IntegrityService.CanInject(press.Context.Window))
                {
                    var element = EditableFocusedElement();
                    var caret = element is null ? null : Caret(element);
                    var native = NativeCaret();
                    result = element is not null && caret is not null &&
                        element.GetRuntimeId().SequenceEqual(anchor.RuntimeId) &&
                        caret.CompareEndpoints(TextPatternRangeEndpoint.Start, anchor.Caret, TextPatternRangeEndpoint.Start) == 0 &&
                        PreviousIs(caret, press.Expected) &&
                        (anchor.NativeCaret == 0 || (native.Caret == anchor.NativeCaret && native.CaretRect.Equals(anchor.Rect))) &&
                        alive.ContainsKey(press.Id) && layouts.Current() == press.Context;
                }
            }
            catch { }
            completed(result);
        })) completed(false);
    }
    public void Forget(Press press)
    {
        alive.TryRemove(press.Id, out _);
        Queue(() => anchors.Remove(press.Id));
    }
    private void Loop()
    {
        while (!stopping)
        {
            if (work.TryTake(out var action, 40)) action();
            if (Stopwatch.GetElapsedTime(lastProbe).TotalMilliseconds >= 100)
            {
                Probe(); lastProbe = Stopwatch.GetTimestamp();
                foreach (var id in anchors.Keys.Where(id => !alive.ContainsKey(id)).ToArray()) anchors.Remove(id);
            }
        }
    }
    private void Probe()
    {
        Volatile.Write(ref safe, null);
        try
        {
            var context = layouts.Current();
            if (context.Focus == 0 || !KeyboardLayoutService.IsRussian(context.Layout) ||
                !IntegrityService.CanInject(context.Window)) return;
            var className = new StringBuilder(128);
            NativeMethods.GetClassName(context.Focus, className, className.Capacity);
            if (className.ToString().Equals("Edit", StringComparison.OrdinalIgnoreCase) &&
                (NativeMethods.GetWindowLong(context.Focus, -16) & 0x20) != 0) return; // ES_PASSWORD
            var element = EditableFocusedElement();
            if (element is null || layouts.Current() != context) return;
            Volatile.Write(ref safe, new(context, element.GetRuntimeId(), Stopwatch.GetTimestamp()));
        }
        catch { }
    }
    private static AutomationElement? EditableFocusedElement()
    {
        var element = AutomationElement.FocusedElement;
        if (element is null || element.Current.IsPassword || !element.Current.IsEnabled || !element.Current.HasKeyboardFocus)
            return null;
        if (!element.TryGetCurrentPattern(TextPattern.Pattern, out var raw)) return null;
        var text = (TextPattern)raw;
        var readOnly = text.DocumentRange.GetAttributeValue(TextPattern.IsReadOnlyAttribute);
        // Unknown read-only status is not sufficient evidence of an editable field.
        return readOnly is bool value && !value ? element : null;
    }
    private static TextPatternRange? Caret(AutomationElement element)
    {
        var text = (TextPattern)element.GetCurrentPattern(TextPattern.Pattern);
        var selection = text.GetSelection();
        return selection.Length == 1 && selection[0].CompareEndpoints(
            TextPatternRangeEndpoint.Start, selection[0], TextPatternRangeEndpoint.End) == 0 ? selection[0] : null;
    }
    private static bool PreviousIs(TextPatternRange caret, char expected)
    {
        var previous = caret.Clone();
        if (previous.MoveEndpointByUnit(TextPatternRangeEndpoint.Start, TextUnit.Character, -1) != -1) return false;
        // At most two characters, solely for a transient equality check. Never logged or retained.
        return previous.GetText(2) == expected.ToString();
    }
    private static NativeMethods.GuiThreadInfo NativeCaret()
    {
        var info = new NativeMethods.GuiThreadInfo { Size = (uint)Marshal.SizeOf<NativeMethods.GuiThreadInfo>() };
        NativeMethods.GetGUIThreadInfo(0, ref info);
        return info;
    }
    public void Dispose() { stopping = true; work.CompleteAdding(); worker.Join(200); }
}
