using BashkortKeyboard.Core;

int passed = 0;
void Test(string name, Action action)
{
    try { action(); Console.WriteLine($"PASS {name}"); passed++; }
    catch (Exception e) { Console.Error.WriteLine($"FAIL {name}: {e.Message}"); Environment.Exit(1); }
}
void Check(bool value, string message = "assertion failed") { if (!value) throw new Exception(message); }
Mapping o = Mapping.All.Single(m => m.Russian == 'о');
InputContext context = new(1, 2, 0x0419);
(LongPressService engine, FakeHost host, AppSettings settings) Setup()
{
    var host = new FakeHost(); return (new(host), host, new());
}
// MVP: only О is supplied to the engine in these cases.
Test("MVP: short O passes immediately", () =>
{
    var (engine, host, settings) = Setup();
    Check(!engine.KeyDown(o.VirtualKey, o, 'о', context, settings, false));
    host.Advance(200); engine.KeyUp(o.VirtualKey); host.Advance(1000);
    Check(host.Replacements.Count == 0);
});
Test("MVP: fast oooo keeps all four first downs", () =>
{
    var (engine, host, settings) = Setup();
    for (int i = 0; i < 4; i++)
    { Check(!engine.KeyDown(o.VirtualKey, o, 'о', context, settings, false)); host.Advance(50); engine.KeyUp(o.VirtualKey); }
    host.Advance(500); Check(host.Replacements.Count == 0);
});
Test("MVP: threshold and repeat before threshold", () =>
{
    var (engine, host, settings) = Setup();
    engine.KeyDown(o.VirtualKey, o, 'о', context, settings, false);
    host.Advance(250); Check(engine.KeyDown(o.VirtualKey, o, 'о', context, settings, false));
    host.Advance(199); Check(host.Replacements.Count == 0);
    host.Advance(1); Check(host.Replacements.Single() == 'ө');
});
Test("MVP: Shift case short and held", () =>
{
    var (engine, host, settings) = Setup();
    Check(!engine.KeyDown(o.VirtualKey, o, 'О', context, settings, false)); engine.KeyUp(o.VirtualKey);
    engine.KeyDown(o.VirtualKey, o, 'О', context, settings, false);
    host.Advance(450); Check(host.Replacements.Single() == 'Ө');
});
Test("MVP: RU to EN or focus change cancels", () =>
{
    var (engine, host, settings) = Setup(); engine.KeyDown(o.VirtualKey, o, 'о', context, settings, false);
    host.ContextValid = false; engine.CheckContexts(); host.Advance(500);
    Check(host.Replacements.Count == 0);
    Check(!engine.KeyDown(o.VirtualKey, null, '\0', new(1, 2, 0x409), settings, false));
});
Test("MVP: shortcuts pass and invalidate hold", () =>
{
    foreach (int key in new[] { 0x11, 0x12, 0x5B })
    {
        var (engine, host, settings) = Setup(); engine.KeyDown(o.VirtualKey, o, 'о', context, settings, false);
        Check(!engine.KeyDown(key, null, '\0', context, settings, true));
        host.Advance(500); Check(host.Replacements.Count == 0);
        Check(!engine.KeyDown(o.VirtualKey, o, 'о', context, settings, true));
    }
});
Test("MVP: hold > one second inserts once", () =>
{
    var (engine, host, settings) = Setup(); engine.KeyDown(o.VirtualKey, o, 'о', context, settings, false);
    for (int i = 0; i < 30; i++) { host.Advance(50); Check(engine.KeyDown(o.VirtualKey, o, 'о', context, settings, false)); }
    Check(host.Replacements.Count == 1); engine.KeyUp(o.VirtualKey);
    Check(!engine.KeyDown(o.VirtualKey, o, 'о', context, settings, false));
});
Test("all mappings and Caps/Shift XOR results", () =>
{
    foreach (var mapping in Mapping.All)
    foreach (char source in new[] { mapping.Russian, char.ToUpperInvariant(mapping.Russian) })
    {
        var (engine, host, settings) = Setup(); engine.KeyDown(mapping.VirtualKey, mapping, source, context, settings, false);
        host.Advance(450); Check(host.Replacements.Single() == (char.IsUpper(source) ? char.ToUpperInvariant(mapping.Bashkir) : mapping.Bashkir));
    }
});
Test("fast word preserves order without replacements", () =>
{
    var (engine, host, settings) = Setup(); var output = new List<char>();
    foreach (char character in "яратам")
    {
        var mapping = Mapping.All.FirstOrDefault(m => m.Russian == character); int key = mapping?.VirtualKey ?? character;
        if (!engine.KeyDown(key, mapping, character, context, settings, false)) output.Add(character);
        host.Advance(45); engine.KeyUp(key);
    }
    host.Advance(1000); Check(new string(output.ToArray()) == "яратам" && host.Replacements.Count == 0);
});
Test("overlapping holds only replace most recent", () =>
{
    var (engine, host, settings) = Setup(); var a = Mapping.All[0];
    engine.KeyDown(o.VirtualKey, o, 'о', context, settings, false); host.Advance(100);
    engine.KeyDown(a.VirtualKey, a, 'а', context, settings, false); host.Advance(450);
    Check(host.Replacements.SequenceEqual(new[] { 'ә' }));
});
Test("two consecutive different holds", () =>
{
    var (engine, host, settings) = Setup();
    foreach (var mapping in new[] { o, Mapping.All[0] })
    { engine.KeyDown(mapping.VirtualKey, mapping, mapping.Russian, context, settings, false); host.Advance(450); engine.KeyUp(mapping.VirtualKey); }
    Check(host.Replacements.SequenceEqual(new[] { 'ө', 'ә' }));
});
Test("successful replacement never repeats Russian after other input", () =>
{
    var (engine, host, settings) = Setup(); engine.KeyDown(o.VirtualKey, o, 'о', context, settings, false);
    host.Advance(450); engine.KeyDown(0x20, null, ' ', context, settings, false);
    Check(engine.KeyDown(o.VirtualKey, o, 'о', context, settings, false));
    Check(!engine.KeyDown(o.VirtualKey, o, 'о', context, settings, true));
});
Test("mouse, external input or settings invalidation", () =>
{
    var (engine, host, settings) = Setup(); engine.KeyDown(o.VirtualKey, o, 'о', context, settings, false);
    host.Advance(400); engine.CancelAll(); host.Advance(500); Check(host.Replacements.Count == 0);
    Check(!engine.KeyDown(o.VirtualKey, o, 'о', context, settings, false));
});
Test("missing caret or password safety proof skips", () =>
{
    var (engine, host, settings) = Setup(); host.ProofValid = false;
    engine.KeyDown(o.VirtualKey, o, 'о', context, settings, false); host.Advance(450);
    Check(host.Replacements.Count == 0); Check(!engine.KeyDown(o.VirtualKey, o, 'о', context, settings, false));
});
Test("late async proof after up or another key cannot replace", () =>
{
    foreach (bool release in new[] { true, false })
    {
        var (engine, host, settings) = Setup(); host.DelayProof = true;
        engine.KeyDown(o.VirtualKey, o, 'о', context, settings, false); host.Advance(450);
        if (release) engine.KeyUp(o.VirtualKey); else engine.KeyDown(0x20, null, ' ', context, settings, false);
        host.Pending!(true); Check(host.Replacements.Count == 0);
    }
});
Test("old proof after same key pressed again cannot replace", () =>
{
    var (engine, host, settings) = Setup(); host.DelayProof = true;
    engine.KeyDown(o.VirtualKey, o, 'о', context, settings, false); host.Advance(450); var old = host.Pending;
    engine.KeyUp(o.VirtualKey); engine.KeyDown(o.VirtualKey, o, 'о', context, settings, false);
    old!(true); Check(host.Replacements.Count == 0);
});
Test("disabled and custom-layout mismatch never intercepted", () =>
{
    var (engine, host, settings) = Setup(); settings.Enabled = false;
    Check(!engine.KeyDown(o.VirtualKey, o, 'о', context, settings, false)); engine.KeyUp(o.VirtualKey);
    settings.Enabled = true; settings.Mappings[o.VirtualKey] = false;
    Check(!engine.KeyDown(o.VirtualKey, o, 'о', context, settings, false)); engine.KeyUp(o.VirtualKey);
    settings.Mappings[o.VirtualKey] = true;
    Check(!engine.KeyDown(o.VirtualKey, o, 'x', context, settings, false)); host.Advance(500); Check(host.Replacements.Count == 0);
});
Console.WriteLine($"{passed} scenario groups passed.");

sealed class FakeHost : IPressHost
{
    private sealed class Scheduled(long due, Action action) : IDisposable
    { public long Due = due; public Action Action = action; public bool Cancelled; public void Dispose() => Cancelled = true; }
    private readonly List<Scheduled> timers = new();
    private long now;
    public bool ContextValid = true, ProofValid = true, DelayProof;
    public Action<bool>? Pending;
    public List<char> Replacements = new();
    public IDisposable Schedule(int milliseconds, Action action) { var item = new Scheduled(now + milliseconds, action); timers.Add(item); return item; }
    public void Advance(int milliseconds)
    {
        long end = now + milliseconds;
        while (timers.Where(t => !t.Cancelled && t.Due <= end).OrderBy(t => t.Due).FirstOrDefault() is { } item)
        { now = item.Due; item.Cancelled = true; item.Action(); }
        now = end;
    }
    public void Capture(Press press) { }
    public void Validate(Press press, Action<bool> completed) { if (DelayProof) Pending = completed; else completed(ProofValid); }
    public bool IsContextValid(Press press) => ContextValid;
    public bool Replace(Press press) { Replacements.Add(press.Replacement); return true; }
    public void Forget(Press press) { }
    public void Log(string eventName) { }
}
