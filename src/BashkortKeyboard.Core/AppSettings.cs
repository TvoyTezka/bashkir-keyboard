namespace BashkortKeyboard.Core;

public sealed class AppSettings
{
    public bool Enabled { get; set; } = true;
    public int HoldMilliseconds { get; set; } = 450;
    public bool StartWithWindows { get; set; }
    public bool ShowTrayIcon { get; set; } = true;
    public bool DiagnosticLogging { get; set; }
    public Dictionary<int, bool> Mappings { get; set; } = Mapping.All.ToDictionary(m => m.VirtualKey, _ => true);
    public void Normalize()
    {
        HoldMilliseconds = Math.Clamp(HoldMilliseconds, 250, 800);
        Mappings ??= new();
        foreach (var mapping in Mapping.All) Mappings.TryAdd(mapping.VirtualKey, true);
    }
}
