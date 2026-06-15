using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using BoseSoundTouchBridge.Localization;
using BoseSoundTouchBridge.Models;
using BoseSoundTouchBridge.Services;

namespace BoseSoundTouchBridge;

public partial class SpotifySettingsWindow : Window
{
    private readonly ObservableCollection<SpotifyDevice> _devices = new();
    private bool _initializing = true;

    public SpotifySettingsWindow()
    {
        InitializeComponent();
        DeviceCombo.ItemsSource = _devices;
        Loaded += async (_, _) => await InitAsync();
    }

    private async Task InitAsync()
    {
        var cfg = App.Current.Settings.Spotify;
        ClientIdBox.Text = cfg.ClientId;
        UpdateConnectedState(cfg);

        if (cfg.IsConnected)
        {
            try { await LoadDevicesAsync(cfg); }
            catch (Exception ex) { HintText.Text = L.SpotifyDeviceLoadErr(ex.Message); }
        }

        _initializing = false;
    }

    private void UpdateConnectedState(SpotifyConfig cfg)
    {
        if (cfg.IsConnected)
        {
            StatusDot.Fill = new SolidColorBrush(Color.FromRgb(29, 185, 84));
            StatusText.Text = string.IsNullOrEmpty(cfg.UserName)
                ? L.SpotifyConnectedNoUser
                : L.SpotifyConnectedAs(cfg.UserName);
            DisconnectButton.IsEnabled = true;
            ConnectButton.Content = L.SpotifyReconnectAction;
        }
        else
        {
            StatusDot.Fill = new SolidColorBrush(Color.FromRgb(128, 128, 128));
            StatusText.Text = L.SpotifyNotConnected;
            DisconnectButton.IsEnabled = false;
            ConnectButton.Content = L.SpotifyConnectAction;
        }
    }

    private async Task LoadDevicesAsync(SpotifyConfig cfg)
    {
        HintText.Text = L.SpotifyDevicesLoading;
        _devices.Clear();
        try
        {
            var devs = await App.Current.Spotify.GetDevicesAsync();
            foreach (var d in devs) _devices.Add(d);
            HintText.Text = devs.Count == 0
                ? L.SpotifyDevicesNone
                : L.SpotifyDevicesCount(devs.Count);

            if (!string.IsNullOrEmpty(cfg.DeviceId))
            {
                var match = _devices.FirstOrDefault(d => d.Id == cfg.DeviceId);
                if (match is not null) DeviceCombo.SelectedItem = match;
            }
        }
        catch (Exception ex)
        {
            HintText.Text = L.RadioErrorPrefix(ex.Message);
        }
    }

    private async void Connect_Click(object sender, RoutedEventArgs e)
    {
        var clientId = ClientIdBox.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(clientId))
        {
            HintText.Text = L.SpotifyGiveClientId;
            return;
        }

        ConnectButton.IsEnabled = false;
        HintText.Text = L.SpotifyWaitingLogin;

        try
        {
            var api = App.Current.Spotify;
            var (refresh, user) = await api.AuthorizeAsync(clientId);

            var cfg = App.Current.Settings.Spotify;
            cfg.ClientId = clientId;
            cfg.RefreshToken = refresh;
            cfg.UserName = user;
            App.Current.SaveSettings();

            UpdateConnectedState(cfg);
            HintText.Text = L.SpotifyConnectedOk;
            await LoadDevicesAsync(cfg);
        }
        catch (Exception ex)
        {
            HintText.Text = L.RadioErrorPrefix(ex.Message);
        }
        finally
        {
            ConnectButton.IsEnabled = true;
        }
    }

    private void Disconnect_Click(object sender, RoutedEventArgs e)
    {
        var cfg = App.Current.Settings.Spotify;
        cfg.RefreshToken = "";
        cfg.UserName = "";
        cfg.DeviceId = "";
        cfg.DeviceName = "";
        App.Current.SaveSettings();
        _devices.Clear();
        UpdateConnectedState(cfg);
        HintText.Text = L.SpotifyDisconnected;
    }

    private async void RefreshDevices_Click(object sender, RoutedEventArgs e)
    {
        var cfg = App.Current.Settings.Spotify;
        if (!cfg.IsConnected)
        {
            HintText.Text = L.SpotifyConnectFirst;
            return;
        }
        await LoadDevicesAsync(cfg);
    }

    private async void TestDevice_Click(object sender, RoutedEventArgs e)
    {
        if (DeviceCombo.SelectedItem is not SpotifyDevice dev)
        {
            HintText.Text = L.SpotifySelectDeviceFirst;
            return;
        }
        HintText.Text = L.SpotifySwitchingTo(dev.Name);
        try
        {
            await App.Current.Spotify.TransferPlaybackAsync(dev.Id, play: false);
            HintText.Text = L.SpotifyTransferredTo(dev.Name);
        }
        catch (Exception ex)
        {
            HintText.Text = L.RadioErrorPrefix(ex.Message);
        }
    }

    private void DeviceCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing) return;
        if (DeviceCombo.SelectedItem is not SpotifyDevice dev) return;
        var cfg = App.Current.Settings.Spotify;
        cfg.DeviceId = dev.Id;
        cfg.DeviceName = dev.Name;
        App.Current.SaveSettings();
        HintText.Text = L.SpotifyDeviceSelected(dev.Name);
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo { FileName = e.Uri.ToString(), UseShellExecute = true }); }
        catch { }
        e.Handled = true;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
