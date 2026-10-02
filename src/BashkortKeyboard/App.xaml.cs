using System.Windows;
using BashkortKeyboard.Services;

namespace BashkortKeyboard;

public partial class App : System.Windows.Application
{
    private Mutex? instance;
    private EventWaitHandle? showEvent;
    private RegisteredWaitHandle? showWait;
    private DiagnosticLog? log;
    private GlobalKeyboardHandler? keyboard;
    private TrayService? tray;
    private MainWindow? window;
    private readonly SettingsStore store = new();
    private AppSettings settings = new();
    public bool Exiting { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        instance = new Mutex(true, @"Local\BashkortKeyboard.SingleInstance", out bool created);
        showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, @"Local\BashkortKeyboard.ShowSettings");
        if (!created) { showEvent.Set(); Shutdown(); return; }
        try
        {
            settings = store.Load();
            log = new(store.DirectoryPath); log.Enable(settings.DiagnosticLogging);
            try { StartupService.MigrateLegacy(settings.StartWithWindows); }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Runtime.InteropServices.COMException or System.Security.SecurityException)
            { log.Write("startup migration failed", true); }
            keyboard = new(SettingsStore.Copy(settings), log);
            window = new MainWindow(this, settings);
            MainWindow = window;
            tray = new(ShowSettings, ToggleEnabled, ExitApplication);
            tray.Update(settings);
            showWait = ThreadPool.RegisterWaitForSingleObject(showEvent,
                (_, _) => Dispatcher.BeginInvoke(ShowSettings), null, Timeout.Infinite, false);
            if (!e.Args.Contains("--background") || !settings.ShowTrayIcon) ShowSettings();
            log.Write("application started", true);
        }
        catch (Exception)
        {
            MessageBox.Show("Не удалось запустить Башҡорт Keyboard. Проверьте доступ к настройкам и возможность установки глобальных обработчиков ввода.",
                "Башҡорт Keyboard", MessageBoxButton.OK, MessageBoxImage.Error);
            ExitApplication();
        }
    }
    public void ShowSettings()
    {
        if (window is null || Exiting) return;
        window.Show(); window.WindowState = WindowState.Normal; window.Activate();
    }
    public void ToggleEnabled()
    {
        var next = SettingsStore.Copy(settings); next.Enabled = !next.Enabled;
        if (Apply(next)) window?.LoadSettings(settings);
    }
    public bool Apply(AppSettings next)
    {
        try
        {
            if (settings.StartWithWindows != next.StartWithWindows) SettingsStore.ApplyStartup(next.StartWithWindows);
            store.Save(next); settings = SettingsStore.Copy(next);
            log?.Enable(settings.DiagnosticLogging);
            keyboard?.Update(SettingsStore.Copy(settings)); tray?.Update(settings);
            if (!settings.ShowTrayIcon && window is not null && !window.IsVisible) ShowSettings();
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.Security.SecurityException or System.Runtime.InteropServices.COMException)
        {
            MessageBox.Show("Не удалось сохранить настройки или изменить автозапуск. Проверьте права доступа.", "Башҡорт Keyboard", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
    }
    public bool TrayVisible => settings.ShowTrayIcon;
    public void ExitApplication() { Exiting = true; Shutdown(); }
    protected override void OnExit(ExitEventArgs e)
    {
        Exiting = true; showWait?.Unregister(null); tray?.Dispose(); keyboard?.Dispose();
        log?.Write("application stopped", true); log?.Dispose();
        showEvent?.Dispose(); instance?.Dispose(); base.OnExit(e);
    }
}
