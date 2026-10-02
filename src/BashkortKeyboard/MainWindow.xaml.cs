using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using BashkortKeyboard.Services;

namespace BashkortKeyboard;

public partial class MainWindow : Window
{
    private readonly App application;
    private bool loading = true;
    private readonly Dictionary<int, CheckBox> mappings = new();
    private readonly DispatcherTimer saveTimer;
    public MainWindow(App application, AppSettings settings)
    {
        this.application = application;
        InitializeComponent();
        foreach (var mapping in Mapping.All)
        {
            var box = new CheckBox { Content = mapping.Label, FontSize = 20, Margin = new Thickness(0, 8, 12, 8) };
            System.Windows.Automation.AutomationProperties.SetName(box, $"Соответствие {mapping.Label}");
            box.Checked += SettingsChanged; box.Unchecked += SettingsChanged;
            mappings.Add(mapping.VirtualKey, box); MappingGrid.Children.Add(box);
        }
        saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(180) };
        saveTimer.Tick += (_, _) => { saveTimer.Stop(); Save(); };
        LoadSettings(settings);
    }
    public void LoadSettings(AppSettings settings)
    {
        loading = true;
        EnabledBox.IsChecked = settings.Enabled; DelaySlider.Value = settings.HoldMilliseconds;
        StartupBox.IsChecked = settings.StartWithWindows; TrayBox.IsChecked = settings.ShowTrayIcon;
        LoggingBox.IsChecked = settings.DiagnosticLogging;
        foreach (var pair in mappings) pair.Value.IsChecked = settings.Mappings.GetValueOrDefault(pair.Key);
        DelayLabel.Text = $"{settings.HoldMilliseconds} мс";
        loading = false;
    }
    private void SettingsChanged(object sender, RoutedEventArgs e)
    {
        if (loading) return;
        saveTimer.Stop(); saveTimer.Start();
    }
    private void DelayChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (DelayLabel is not null) DelayLabel.Text = $"{(int)e.NewValue} мс";
        if (!loading) SettingsChanged(sender, e);
    }
    private void Save()
    {
        var next = new AppSettings
        {
            Enabled = EnabledBox.IsChecked == true, HoldMilliseconds = (int)DelaySlider.Value,
            StartWithWindows = StartupBox.IsChecked == true, ShowTrayIcon = TrayBox.IsChecked == true,
            DiagnosticLogging = LoggingBox.IsChecked == true,
            Mappings = mappings.ToDictionary(p => p.Key, p => p.Value.IsChecked == true)
        };
        SaveStatus.Text = application.Apply(next) ? "Настройки сохранены" : "Не удалось сохранить настройки";
    }
    private void ResetClick(object sender, RoutedEventArgs e)
    {
        saveTimer.Stop(); var defaults = new AppSettings();
        if (application.Apply(defaults)) LoadSettings(defaults);
    }
    private void ExitClick(object sender, RoutedEventArgs e) { saveTimer.Stop(); Save(); application.ExitApplication(); }
    protected override void OnClosing(CancelEventArgs e)
    {
        if (!application.Exiting)
        {
            saveTimer.Stop(); Save(); e.Cancel = true;
            if (application.TrayVisible) Hide(); else WindowState = WindowState.Minimized;
        }
        base.OnClosing(e);
    }
}
