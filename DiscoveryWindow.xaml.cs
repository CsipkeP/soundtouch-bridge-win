using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BoseSoundTouchBridge.Localization;
using BoseSoundTouchBridge.Services;

namespace BoseSoundTouchBridge;

public partial class DiscoveryWindow : Window
{
    private readonly ObservableCollection<DiscoveredDevice> _devices = new();
    private CancellationTokenSource? _cts;

    public DiscoveredDevice? Selected { get; private set; }

    public DiscoveryWindow()
    {
        InitializeComponent();
        DevicesList.ItemsSource = _devices;
        Loaded += async (_, _) => await StartDiscoveryAsync();
    }

    private async Task StartDiscoveryAsync()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        _devices.Clear();
        OkButton.IsEnabled = false;
        StatusText.Text = L.DiscoverySearching;
        ProgressBar.IsIndeterminate = true;
        ProgressBar.Visibility = Visibility.Visible;

        var discovery = new DeviceDiscovery();
        discovery.DeviceFound += (_, dev) =>
        {
            Dispatcher.Invoke(() =>
            {
                if (_devices.Any(d => string.Equals(d.Ip, dev.Ip, StringComparison.OrdinalIgnoreCase)))
                    return;
                _devices.Add(dev);
                StatusText.Text = L.DiscoveryFoundShort(_devices.Count);
            });
        };

        try
        {
            await discovery.DiscoverAsync(TimeSpan.FromSeconds(4), ct);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            StatusText.Text = L.DiscoveryError(ex.Message);
        }

        ProgressBar.IsIndeterminate = false;
        ProgressBar.Visibility = Visibility.Collapsed;
        StatusText.Text = _devices.Count == 0
            ? L.DiscoveryNoneFound
            : L.DiscoveryFound(_devices.Count);
    }

    private void DevicesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        OkButton.IsEnabled = DevicesList.SelectedItem is not null;
    }

    private void DevicesList_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DevicesList.SelectedItem is not null) Ok_Click(sender, e);
    }

    private async void Research_Click(object sender, RoutedEventArgs e)
    {
        await StartDiscoveryAsync();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Selected = null;
        DialogResult = false;
        Close();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        Selected = DevicesList.SelectedItem as DiscoveredDevice;
        if (Selected is null) return;
        DialogResult = true;
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        try { _cts?.Cancel(); } catch { }
        base.OnClosed(e);
    }
}
