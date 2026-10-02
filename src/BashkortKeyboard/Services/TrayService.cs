using System.Drawing;
using System.Windows.Forms;

namespace BashkortKeyboard.Services;

internal sealed class TrayService : IDisposable
{
    private readonly NotifyIcon icon;
    private readonly Icon trayIcon;
    private readonly ContextMenuStrip menu = new();
    private readonly ToolStripMenuItem enabled;
    public TrayService(Action open, Action toggle, Action exit)
    {
        enabled = new("Включить башкирский ввод"); enabled.Click += (_, _) => toggle();
        menu.Items.Add(enabled);
        menu.Items.Add("Открыть настройки", null, (_, _) => open());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Выход", null, (_, _) => exit());
        using var iconStream = typeof(TrayService).Assembly.GetManifestResourceStream("BashkortKeyboard.Assets.App.ico")
            ?? throw new InvalidOperationException("Не найден ресурс иконки приложения.");
        using var sourceIcon = new Icon(iconStream);
        trayIcon = new Icon(sourceIcon, SystemInformation.SmallIconSize);
        icon = new() { Icon = trayIcon, ContextMenuStrip = menu, Text = "Башҡорт Keyboard" };
        icon.DoubleClick += (_, _) => open();
    }
    public void Update(AppSettings settings)
    {
        enabled.Checked = settings.Enabled;
        enabled.Text = settings.Enabled ? "Выключить башкирский ввод" : "Включить башкирский ввод";
        icon.Text = settings.Enabled ? "Башҡорт Keyboard — включено" : "Башҡорт Keyboard — выключено";
        icon.Visible = settings.ShowTrayIcon;
    }
    public void Dispose() { icon.Visible = false; icon.Dispose(); trayIcon.Dispose(); menu.Dispose(); }
}
