using System.Diagnostics;
using System.Windows;
using BoseSoundTouchBridge.Localization;
using BoseSoundTouchBridge.Models;
using BoseSoundTouchBridge.Services;
using H.NotifyIcon;
using H.NotifyIcon.Core;

namespace BoseSoundTouchBridge;

public partial class App : Application
{
    private static System.Threading.Mutex? _singleInstanceMutex;
    private TaskbarIcon? _tray;
    private MainWindow? _settingsWindow;
    private SpotifySettingsWindow? _spotifyWindow;
    public BoseClient? Bridge { get; private set; }
    public SpotifyApi Spotify { get; private set; } = null!;
    public AppSettings Settings { get; private set; } = new();

    public static new App Current => (App)Application.Current;

    public void SaveSettings() => SettingsStore.Save(Settings);

    protected override void OnStartup(StartupEventArgs e)
    {
        _singleInstanceMutex = new System.Threading.Mutex(true, "BoseSoundTouchBridge.SingleInstance", out var owned);

        Settings = SettingsStore.Load();
        if (string.IsNullOrWhiteSpace(Settings.Language))
        {
            Settings.Language = L.DetectDefault();
            SettingsStore.Save(Settings);
        }
        L.Initialize(Settings.Language);

        if (!owned)
        {
            MessageBox.Show(L.AppDuplicate, "Bose SoundTouch Bridge",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        DispatcherUnhandledException += (_, ev) =>
        {
            MessageBox.Show(
                $"{ev.Exception.GetType().Name}: {ev.Exception.Message}\n\n{ev.Exception.StackTrace}",
                L.AppErrorTitle,
                MessageBoxButton.OK, MessageBoxImage.Error);
            ev.Handled = true;
        };

        base.OnStartup(e);

        Spotify = new SpotifyApi(
            getConfig: () => Settings.Spotify,
            saveConfig: cfg => { Settings.Spotify = cfg; SaveSettings(); });

        _tray = (TaskbarIcon)FindResource("TrayIcon");
        _tray.ForceCreate();

        Bridge = new BoseClient(Settings, Spotify);
        Bridge.StatusChanged += OnBridgeStatusChanged;
        Bridge.PresetTriggered += OnPresetTriggered;
        Bridge.Start();

        if (string.IsNullOrWhiteSpace(Settings.IpAddress))
        {
            ShowSettingsWindow();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try { Bridge?.Dispose(); } catch { }
        try { _tray?.Dispose(); } catch { }
        try { _singleInstanceMutex?.ReleaseMutex(); } catch { }
        base.OnExit(e);
    }

    public void ApplySettings(AppSettings updated)
    {
        Settings = updated;
        SettingsStore.Save(Settings);
        Bridge?.UpdateSettings(Settings);
    }

    public void SwitchLanguage()
    {
        var newLang = L.Lang == "hu" ? "en" : "hu";
        var result = MessageBox.Show(L.LangChangeRestartText, L.LangChangeRestartTitle,
            MessageBoxButton.OKCancel, MessageBoxImage.Question);
        if (result != MessageBoxResult.OK) return;

        Settings.Language = newLang;
        SettingsStore.Save(Settings);

        var exe = Process.GetCurrentProcess().MainModule?.FileName;
        if (!string.IsNullOrEmpty(exe))
        {
            try { Process.Start(new ProcessStartInfo { FileName = exe, UseShellExecute = true }); }
            catch { }
        }
        Shutdown();
    }

    private void ShowSettings_Click(object sender, RoutedEventArgs e) => ShowSettingsWindow();
    private void TrayIcon_LeftMouseUp(object sender, RoutedEventArgs e) => ShowSettingsWindow();

    private void Reconnect_Click(object sender, RoutedEventArgs e) => Bridge?.Restart();
    private void SpotifySettings_Click(object sender, RoutedEventArgs e) => ShowSpotifyWindow();
    private void ShowHelp_Click(object sender, RoutedEventArgs e) => ShowHelpWindow();

    private void Exit_Click(object sender, RoutedEventArgs e) => Shutdown();

    private HelpWindow? _helpWindow;
    public void ShowHelpWindow()
    {
        if (_helpWindow is null || !_helpWindow.IsLoaded)
        {
            _helpWindow = new HelpWindow
            {
                Owner = _settingsWindow?.IsVisible == true ? _settingsWindow : null
            };
        }
        _helpWindow.Show();
        _helpWindow.Activate();
        _helpWindow.WindowState = WindowState.Normal;
    }

    public void ShowSpotifyWindow()
    {
        if (_spotifyWindow is null || !_spotifyWindow.IsLoaded)
        {
            _spotifyWindow = new SpotifySettingsWindow
            {
                Owner = _settingsWindow?.IsVisible == true ? _settingsWindow : null
            };
        }
        _spotifyWindow.Show();
        _spotifyWindow.Activate();
        _spotifyWindow.WindowState = WindowState.Normal;
    }

    public void ShowSettingsWindow()
    {
        if (_settingsWindow is null)
        {
            _settingsWindow = new MainWindow();
            _settingsWindow.Closing += (_, args) =>
            {
                args.Cancel = true;
                _settingsWindow!.Hide();
            };
        }
        _settingsWindow.Show();
        _settingsWindow.Activate();
        _settingsWindow.WindowState = WindowState.Normal;
    }

    private void OnBridgeStatusChanged(object? sender, BridgeStatusEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            if (_tray is not null) _tray.ToolTipText = L.TrayTooltip(e.Message);
            _settingsWindow?.OnStatus(e);
        });
    }

    private void OnPresetTriggered(object? sender, PresetTriggeredEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            var name = string.IsNullOrWhiteSpace(e.Preset.Name) ? $"Preset {e.PresetId}" : e.Preset.Name;
            var msg = e.Success ? L.NotifyPlaying(name) : L.NotifyError(name, e.Error ?? "");
            if (_tray is not null)
            {
                _tray.ShowNotification(
                    title: "Bose SoundTouch Bridge",
                    message: msg,
                    icon: e.Success ? NotificationIcon.Info : NotificationIcon.Warning);
            }
            _settingsWindow?.OnPresetTriggered(e);
        });
    }
}
