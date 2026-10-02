using System.Collections.Concurrent;
using System.Diagnostics;

namespace BashkortKeyboard.Services;

internal sealed class DiagnosticLog : IDisposable
{
    private readonly BlockingCollection<string> queue = new(256);
    private readonly Thread writer;
    private volatile bool enabled;
    private readonly string path;
    public DiagnosticLog(string directory)
    {
        Directory.CreateDirectory(directory);
        path = Path.Combine(directory, "diagnostics.log");
        writer = new Thread(WriteLoop) { IsBackground = true, Name = "Diagnostic writer" };
        writer.Start();
    }
    public void Enable(bool value) => enabled = value;
    public void Write(string eventName, bool always = false)
    {
        // Callers supply fixed event descriptions only: no keys, characters,
        // window titles, process names, clipboard or document contents.
        if (!always && !enabled) return;
        try { if (!queue.IsAddingCompleted) queue.TryAdd($"{DateTimeOffset.Now:O} {eventName}"); }
        catch (InvalidOperationException) { }
    }
    private void WriteLoop()
    {
        foreach (var line in queue.GetConsumingEnumerable())
        {
            Debug.WriteLine(line);
            try
            {
                if (File.Exists(path) && new FileInfo(path).Length > 1_048_576)
                    File.Move(path, path + ".previous", true);
                File.AppendAllText(path, line + Environment.NewLine);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
    public void Dispose() { queue.CompleteAdding(); writer.Join(300); }
}
