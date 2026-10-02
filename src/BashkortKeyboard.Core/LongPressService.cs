namespace BashkortKeyboard.Core;

public readonly record struct InputContext(nint Window, nint Focus, nint Layout);
public sealed record Press(Guid Id, int Key, char Expected, char Replacement, InputContext Context);

public interface IPressHost
{
    IDisposable Schedule(int milliseconds, Action action);
    void Capture(Press press);
    void Validate(Press press, Action<bool> completed);
    bool IsContextValid(Press press);
    bool Replace(Press press);
    void Forget(Press press);
    void Log(string eventName);
}

// All calls, including timer completions, are serialized on the hook's message thread.
// The first down and every up ALWAYS pass through. Only repeats of an eligible
// uninterrupted hold are swallowed. A cancelled hold resumes ordinary repeat.
public sealed class LongPressService(IPressHost host)
{
    private sealed class State(Press press)
    {
        public Press Press { get; } = press;
        public IDisposable? CaptureTimer;
        public IDisposable? HoldTimer;
        public bool Cancelled;
        public bool Replaced;
    }
    private readonly Dictionary<int, State> states = new();
    private readonly HashSet<int> down = new();

    public bool KeyDown(int key, Mapping? mapping, char translated, InputContext context, AppSettings settings, bool shortcut)
    {
        host.Log("keydown"); // No virtual key or text in logs.
        if (!down.Add(key))
        {
            if (!settings.Enabled || !settings.Mappings.GetValueOrDefault(key)) return false;
            // Once replaced, no Russian repeat until release, even if another
            // printable key or mouse event has cancelled the old context.
            if (states.TryGetValue(key, out var replaced) && replaced.Replaced && !shortcut) return true;
            if (states.TryGetValue(key, out var held) && !held.Cancelled)
            {
                if (!shortcut && host.IsContextValid(held.Press)) return true;
                Cancel(held);
            }
            return false;
        }
        CancelAll(); // Any new key invalidates the previous character's position.
        if (!settings.Enabled || shortcut || mapping is null ||
            !settings.Mappings.GetValueOrDefault(key) ||
            char.ToLowerInvariant(translated) != mapping.Russian)
            return false;
        var upper = char.IsUpper(translated);
        var press = new Press(Guid.NewGuid(), key, translated,
            upper ? char.ToUpperInvariant(mapping.Bashkir) : mapping.Bashkir, context);
        var state = new State(press);
        states[key] = state;
        state.CaptureTimer = host.Schedule(35, () =>
        {
            if (!state.Cancelled && host.IsContextValid(press)) host.Capture(press);
            else Cancel(state);
        });
        state.HoldTimer = host.Schedule(settings.HoldMilliseconds, () => Trigger(state));
        return false;
    }

    public void KeyUp(int key)
    {
        host.Log("keyup");
        down.Remove(key);
        if (states.Remove(key, out var state)) Release(state);
        // Shift release can change case while holding; conservative cancellation.
        if (key is 0x10 or 0xA0 or 0xA1 or 0x11 or 0xA2 or 0xA3 or 0x12 or 0xA4 or 0xA5 or 0x5B or 0x5C)
            CancelAll();
    }

    public void CheckContexts()
    {
        foreach (var state in states.Values)
            if (!state.Cancelled && !host.IsContextValid(state.Press)) Cancel(state);
    }

    public void CancelAll()
    {
        foreach (var state in states.Values) Cancel(state);
    }

    public void Reset()
    {
        foreach (var state in states.Values) Release(state);
        states.Clear();
        down.Clear();
    }

    private void Trigger(State state)
    {
        if (state.Cancelled || !down.Contains(state.Press.Key) || !host.IsContextValid(state.Press))
        { Cancel(state); return; }
        host.Log("long press triggered");
        host.Validate(state.Press, valid =>
        {
            if (state.Cancelled || !states.TryGetValue(state.Press.Key, out var current) ||
                current != state || !down.Contains(state.Press.Key)) return;
            if (!valid || !host.IsContextValid(state.Press) || !host.Replace(state.Press))
            { Cancel(state); host.Log("replacement skipped"); return; }
            state.Replaced = true;
            host.Log("replacement sent");
            host.Forget(state.Press);
        });
    }

    private void Cancel(State state)
    {
        if (state.Cancelled) return;
        // A completed replacement keeps suppressing repeat until up. Changes in
        // context or another key explicitly cancel it, allowing shortcut events.
        state.Cancelled = true;
        Release(state);
        host.Log("hold cancelled");
    }

    private void Release(State state)
    {
        state.CaptureTimer?.Dispose(); state.HoldTimer?.Dispose();
        host.Forget(state.Press);
    }
}
