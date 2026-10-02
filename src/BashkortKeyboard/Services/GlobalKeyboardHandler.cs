using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using BashkortKeyboard.Interop;

namespace BashkortKeyboard.Services;

internal sealed class GlobalKeyboardHandler : IPressHost, IDisposable
{
    private readonly KeyboardLayoutService layouts = new();
    private readonly InputSender sender = new();
    private readonly TextContextGuard guard = new();
    private readonly DiagnosticLog log;
    private readonly Thread thread;
    private readonly ManualResetEventSlim ready = new();
    private readonly NativeMethods.HookProc keyboardCallback, mouseCallback;
    private readonly HashSet<int> physicallyDown = new();
    private readonly LongPressService engine;
    private Dispatcher dispatcher = null!;
    private DispatcherTimer? watchdog;
    private nint keyboardHook, mouseHook;
    private Exception? startError;
    private AppSettings settings;
    private bool disposed;
    private bool capsLock, capsDown;
    private nint lastLayout;

    public GlobalKeyboardHandler(AppSettings settings, DiagnosticLog log)
    {
        this.settings = settings; this.log = log;
        engine = new(this);
        keyboardCallback = KeyboardCallback; mouseCallback = MouseCallback;
        thread = new Thread(Run) { IsBackground = true, Name = "Global keyboard message pump" };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start(); ready.Wait();
        if (startError is not null) { guard.Dispose(); throw new InvalidOperationException("Не удалось установить обработчики ввода.", startError); }
    }
    private void Run()
    {
        dispatcher = Dispatcher.CurrentDispatcher;
        try
        {
            capsLock = (NativeMethods.GetKeyState(0x14) & 1) != 0;
            keyboardHook = NativeMethods.SetWindowsHookEx(NativeMethods.WhKeyboardLl, keyboardCallback, NativeMethods.GetModuleHandle(null), 0);
            mouseHook = NativeMethods.SetWindowsHookEx(NativeMethods.WhMouseLl, mouseCallback, NativeMethods.GetModuleHandle(null), 0);
            if (keyboardHook == 0 || mouseHook == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
            watchdog = new DispatcherTimer(TimeSpan.FromMilliseconds(20), DispatcherPriority.Input,
                (_, _) => engine.CheckContexts(), dispatcher);
            ready.Set();
            Dispatcher.Run();
        }
        catch (Exception exception) { startError = exception; ready.Set(); }
        finally
        {
            watchdog?.Stop(); engine.Reset();
            if (keyboardHook != 0) NativeMethods.UnhookWindowsHookEx(keyboardHook);
            if (mouseHook != 0) NativeMethods.UnhookWindowsHookEx(mouseHook);
        }
    }
    public void Update(AppSettings updated) => dispatcher.BeginInvoke(() => { engine.CancelAll(); settings = updated; });
    private nint KeyboardCallback(int code, nint message, nint data)
    {
        if (code < 0) return NativeMethods.CallNextHookEx(keyboardHook, code, message, data);
        try
        {
            var input = Marshal.PtrToStructure<NativeMethods.KeyboardHook>(data);
            if (input.Extra == NativeMethods.InjectionTag && (input.Flags & 0x10) != 0)
                return NativeMethods.CallNextHookEx(keyboardHook, code, message, data);
            // Track the toggle from events: GetKeyState on a hook-only thread can
            // become stale because it has no target application's keyboard queue.
            if (input.Vk == 0x14)
            {
                if ((int)message is 0x101 or 0x105) capsDown = false;
                else if ((int)message is 0x100 or 0x104)
                { if (!capsDown) capsLock = !capsLock; capsDown = true; }
            }
            if ((input.Flags & 0x10) != 0)
            {
                engine.CancelAll(); guard.InvalidateFocus(); log.Write("external injected event skipped");
                return NativeMethods.CallNextHookEx(keyboardHook, code, message, data);
            }
            int key = (int)input.Vk;
            if ((int)message is 0x101 or 0x105)
            {
                physicallyDown.Remove(key); engine.KeyUp(key);
            }
            else if ((int)message is 0x100 or 0x104)
            {
                physicallyDown.Add(key); // Async key state is not yet updated in a LL callback.
                bool shortcut = ShortcutHeld();
                if (shortcut || key is 0x09 or 0x1B or 0x0D or 0x21 or 0x22 or 0x23 or 0x24 or 0x25 or 0x26 or 0x27 or 0x28)
                    guard.InvalidateFocus();
                var context = layouts.Current();
                if (lastLayout != context.Layout)
                {
                    lastLayout = context.Layout;
                    log.Write(KeyboardLayoutService.IsRussian(lastLayout) ? "layout RU" : "layout other");
                }
                var mapping = KeyboardLayoutService.IsRussian(context.Layout) && guard.IsEligible(context)
                    ? Mapping.All.FirstOrDefault(m => m.VirtualKey == key) : null;
                bool shift = physicallyDown.Contains(0xA0) || physicallyDown.Contains(0xA1) || KeyboardLayoutService.Down(0x10);
                char translated = mapping is null ? '\0' : layouts.Translate(input.Vk, input.Scan, context.Layout, shift, capsLock);
                if (engine.KeyDown(key, mapping, translated, context, settings, shortcut)) return 1;
            }
        }
        catch { engine.CancelAll(); log.Write("hook event skipped after error", true); }
        return NativeMethods.CallNextHookEx(keyboardHook, code, message, data);
    }
    private nint MouseCallback(int code, nint message, nint data)
    {
        // Button down (including right, middle, X) and wheel can move the caret/focus.
        if (code >= 0 && (int)message is 0x201 or 0x204 or 0x207 or 0x20B or 0x20A or 0x20E)
        { engine.CancelAll(); guard.InvalidateFocus(); }
        return NativeMethods.CallNextHookEx(mouseHook, code, message, data);
    }
    private bool ShortcutHeld() => physicallyDown.Any(k => k is 0x11 or 0xA2 or 0xA3 or 0x12 or 0xA4 or 0xA5 or 0x5B or 0x5C) || KeyboardLayoutService.ShortcutDown();
    public bool IsContextValid(Press press) => KeyboardLayoutService.Down(press.Key) && !ShortcutHeld() && layouts.Current() == press.Context;
    public IDisposable Schedule(int milliseconds, Action action)
    {
        var timer = new DispatcherTimer(DispatcherPriority.Input, dispatcher) { Interval = TimeSpan.FromMilliseconds(milliseconds) };
        timer.Tick += (_, _) => { timer.Stop(); action(); };
        timer.Start(); return new TimerLease(timer);
    }
    private sealed class TimerLease(DispatcherTimer timer) : IDisposable { public void Dispose() => timer.Stop(); }
    public void Capture(Press press) => guard.Capture(press);
    public void Validate(Press press, Action<bool> completed)
    {
        long started = Stopwatch.GetTimestamp();
        bool finished = false;
        var expiry = Schedule(180, () => { if (!finished) { finished = true; completed(false); } });
        guard.Validate(press, valid =>
        {
            if (disposed || dispatcher.HasShutdownStarted) return;
            dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
            {
                if (finished) return;
                finished = true; expiry.Dispose();
                completed(valid && Stopwatch.GetElapsedTime(started).TotalMilliseconds <= 180);
            }));
        });
    }
    public bool Replace(Press press) => IsContextValid(press) && sender.Replace(press.Replacement);
    public void Forget(Press press) => guard.Forget(press);
    public void Log(string eventName) => log.Write(eventName);
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        dispatcher.BeginInvokeShutdown(DispatcherPriority.Send);
        thread.Join(500); guard.Dispose(); ready.Dispose();
    }
}
