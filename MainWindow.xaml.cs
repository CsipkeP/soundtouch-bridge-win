using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using BoseSoundTouchBridge.Localization;
using BoseSoundTouchBridge.Models;
using BoseSoundTouchBridge.Services;

namespace BoseSoundTouchBridge;

public partial class MainWindow : Window
{
    public ObservableCollection<PresetVm> PresetVms { get; } = new();

    private bool _suppressVolumeUpdate;
    private CancellationTokenSource? _volumeDebounceCts;

    public MainWindow()
    {
        InitializeComponent();
        LoadFromSettings(App.Current.Settings);
        PresetItems.ItemsSource = PresetVms;
        LangButton.Content = L.Lang == "hu" ? "EN" : "HU";

        var state = App.Current.Bridge?.State ?? BridgeState.Disconnected;
        OnStatus(new BridgeStatusEventArgs(state, FriendlyStateMessage(state)));

        IsVisibleChanged += async (_, e) =>
        {
            if (!IsVisible) return;
            var current = App.Current.Bridge?.State ?? BridgeState.Disconnected;
            OnStatus(new BridgeStatusEventArgs(current, FriendlyStateMessage(current)));
            if (!string.IsNullOrWhiteSpace(IpTextBox.Text))
                await RefreshDeviceStateAsync();
        };
    }

    public void OnStatus(BridgeStatusEventArgs e)
    {
        StatusText.Text = e.Message;
        StatusDot.Fill = e.State switch
        {
            BridgeState.Connected => new SolidColorBrush(Color.FromRgb(46, 160, 67)),
            BridgeState.Connecting => new SolidColorBrush(Color.FromRgb(218, 165, 32)),
            BridgeState.Error => new SolidColorBrush(Color.FromRgb(207, 34, 46)),
            _ => new SolidColorBrush(Color.FromRgb(128, 128, 128))
        };
    }

    public void OnPresetTriggered(PresetTriggeredEventArgs e)
    {
        var name = string.IsNullOrWhiteSpace(e.Preset.Name) ? $"Preset {e.PresetId}" : e.Preset.Name;
        HintText.Text = e.Success ? $"▶ {name}" : $"⚠ {name}: {e.Error}";
    }

    private void LangToggle_Click(object sender, RoutedEventArgs e) => App.Current.SwitchLanguage();

    private void LoadFromSettings(AppSettings s)
    {
        IpTextBox.Text = s.IpAddress;
        PresetVms.Clear();
        for (int i = 0; i < 6; i++)
        {
            PresetVms.Add(new PresetVm
            {
                Index = i + 1,
                Name = s.Presets[i].Name,
                Url = s.Presets[i].Url
            });
        }
    }

    private AppSettings BuildSettings()
    {
        var s = new AppSettings
        {
            IpAddress = (IpTextBox.Text ?? "").Trim(),
            Presets = PresetVms.Select(p => new Preset
            {
                Name = (p.Name ?? "").Trim(),
                Url = (p.Url ?? "").Trim()
            }).ToList()
        };
        s.EnsureSixPresets();
        return s;
    }

    private async void Search_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new DiscoveryWindow { Owner = this };
        var ok = dlg.ShowDialog();
        if (ok == true && dlg.Selected is not null)
        {
            IpTextBox.Text = dlg.Selected.Ip;
            HintText.Text = L.HintSelected(dlg.Selected.Name, dlg.Selected.Ip);
            await RefreshDeviceStateAsync();
        }
    }

    private void BrowseRadio_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not PresetVm vm) return;

        var dlg = new RadioPickerWindow { Owner = this };
        if (dlg.ShowDialog() == true && dlg.Selected is not null)
        {
            var s = dlg.Selected;
            vm.Name = s.Name;
            vm.Url = s.PlayUrl;
            HintText.Text = L.HintPresetSet(vm.Index, s.Name);
        }
    }

    private async void TestPreset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not PresetVm vm) return;

        var ip = (IpTextBox.Text ?? "").Trim();
        if (string.IsNullOrWhiteSpace(ip))
        {
            HintText.Text = L.HintGiveIpFirst;
            return;
        }
        if (string.IsNullOrWhiteSpace(vm.Url))
        {
            HintText.Text = L.HintPresetNoUrl(vm.Index);
            return;
        }

        HintText.Text = L.HintTestingPreset(vm.Index);
        try
        {
            var url = vm.Url.Trim();
            var spotifyUri = SpotifyApi.ParseUri(url);
            if (spotifyUri is not null)
            {
                var sp = App.Current.Settings.Spotify;
                if (string.IsNullOrEmpty(sp.DeviceId))
                {
                    HintText.Text = L.HintSpotifyNoDevice;
                    return;
                }
                await App.Current.Spotify.PlayAsync(sp.DeviceId, spotifyUri);
            }
            else
            {
                var upnp = new UpnpClient(ip);
                await upnp.PlayUrlAsync(url,
                    string.IsNullOrWhiteSpace(vm.Name) ? $"Preset {vm.Index}" : vm.Name);
            }
            HintText.Text = L.HintPlayStarted(vm.Index);
        }
        catch (Exception ex)
        {
            HintText.Text = L.HintError(ex.Message);
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var s = BuildSettings();
        if (!string.IsNullOrEmpty(s.IpAddress) && !LooksLikeIp(s.IpAddress) && !LooksLikeHost(s.IpAddress))
        {
            HintText.Text = L.HintInvalidIp;
            return;
        }
        App.Current.ApplySettings(s);
        HintText.Text = L.HintSaved(SettingsStore.SettingsPath);
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Hide();

    private void ReconnectInline_Click(object sender, RoutedEventArgs e)
    {
        App.Current.Bridge?.Restart();
        HintText.Text = L.HintReconnectStarted;
    }

    private void SpotifySettings_Click(object sender, RoutedEventArgs e)
    {
        App.Current.ShowSpotifyWindow();
    }

    private void ShowHelp_Click(object sender, RoutedEventArgs e)
    {
        App.Current.ShowHelpWindow();
    }

    private SoundtouchApiClient? CreateApi()
    {
        var ip = (IpTextBox.Text ?? "").Trim();
        if (string.IsNullOrWhiteSpace(ip))
        {
            HintText.Text = L.HintGiveIp;
            return null;
        }
        return new SoundtouchApiClient(ip);
    }

    private async void Power_Click(object sender, RoutedEventArgs e)
    {
        using var api = CreateApi();
        if (api is null) return;
        try
        {
            await api.TogglePowerAsync();
            HintText.Text = L.HintPowerSent;
            await Task.Delay(600);
            await RefreshDeviceStateAsync();
        }
        catch (Exception ex) { HintText.Text = L.HintPowerError(ex.Message); }
    }

    private async void Mute_Click(object sender, RoutedEventArgs e)
    {
        using var api = CreateApi();
        if (api is null) return;
        try
        {
            await api.ToggleMuteAsync();
            HintText.Text = L.HintMuteToggled;
            await Task.Delay(400);
            await RefreshDeviceStateAsync();
        }
        catch (Exception ex) { HintText.Text = L.HintMuteError(ex.Message); }
    }

    private async void VolumeUp_Click(object sender, RoutedEventArgs e)
    {
        using var api = CreateApi();
        if (api is null) return;
        try { await api.VolumeUpAsync(); await Task.Delay(200); await RefreshDeviceStateAsync(); }
        catch (Exception ex) { HintText.Text = L.HintVolumeError(ex.Message); }
    }

    private async void VolumeDown_Click(object sender, RoutedEventArgs e)
    {
        using var api = CreateApi();
        if (api is null) return;
        try { await api.VolumeDownAsync(); await Task.Delay(200); await RefreshDeviceStateAsync(); }
        catch (Exception ex) { HintText.Text = L.HintVolumeError(ex.Message); }
    }

    private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_suppressVolumeUpdate) return;
        var newVol = (int)Math.Round(e.NewValue);
        VolumeLabel.Text = newVol.ToString();

        var ip = (IpTextBox.Text ?? "").Trim();
        if (string.IsNullOrWhiteSpace(ip)) return;

        _volumeDebounceCts?.Cancel();
        _volumeDebounceCts = new CancellationTokenSource();
        var ct = _volumeDebounceCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(180, ct);
                ct.ThrowIfCancellationRequested();
                using var api = new SoundtouchApiClient(ip);
                await api.SetVolumeAsync(newVol, ct);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => HintText.Text = L.HintVolumeError(ex.Message));
            }
        });
    }

    private async void RefreshState_Click(object sender, RoutedEventArgs e)
    {
        await RefreshDeviceStateAsync();
    }

    private async Task RefreshDeviceStateAsync()
    {
        var ip = (IpTextBox.Text ?? "").Trim();
        if (string.IsNullOrWhiteSpace(ip)) return;

        try
        {
            using var api = new SoundtouchApiClient(ip);
            var vol = await api.GetVolumeAsync();
            var source = await api.GetSourceAsync();
            var on = !string.Equals(source, "STANDBY", StringComparison.OrdinalIgnoreCase);

            _suppressVolumeUpdate = true;
            try { VolumeSlider.Value = vol.Level; } finally { _suppressVolumeUpdate = false; }
            VolumeLabel.Text = vol.Level.ToString();

            MuteIcon.Text = vol.Muted ? "" : "";
            MuteButton.ToolTip = vol.Muted ? L.MuteOffTooltip : L.MuteTooltip;

            PowerIcon.Foreground = on
                ? new SolidColorBrush(Color.FromRgb(46, 160, 67))
                : new SolidColorBrush(Color.FromRgb(207, 34, 46));
            PowerButton.ToolTip = on ? L.HintPoweredOn(source) : L.HintPoweredOff;
        }
        catch (Exception ex)
        {
            HintText.Text = L.HintStateError(ex.Message);
            PowerIcon.Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136));
        }
    }

    private static bool LooksLikeIp(string s) =>
        System.Net.IPAddress.TryParse(s, out _);

    private static bool LooksLikeHost(string s) =>
        Uri.CheckHostName(s) is UriHostNameType.Dns or UriHostNameType.IPv4 or UriHostNameType.IPv6;

    private static string FriendlyStateMessage(BridgeState s) => s switch
    {
        BridgeState.Connected => L.StatusConnected,
        BridgeState.Connecting => L.StatusConnecting,
        BridgeState.Error => L.StatusErrorRetry,
        _ => L.StatusStopped
    };
}

public class PresetVm : INotifyPropertyChanged
{
    private string _name = "";
    private string _url = "";

    public int Index { get; set; }
    public string HeaderLabel => $"Preset {Index}";

    public string Name
    {
        get => _name;
        set { if (_name != value) { _name = value; OnPropertyChanged(); } }
    }

    public string Url
    {
        get => _url;
        set { if (_url != value) { _url = value; OnPropertyChanged(); } }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
