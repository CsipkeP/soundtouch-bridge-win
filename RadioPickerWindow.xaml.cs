using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BoseSoundTouchBridge.Localization;
using BoseSoundTouchBridge.Services;

namespace BoseSoundTouchBridge;

public partial class RadioPickerWindow : Window
{
    private readonly RadioBrowserApi _api = new();
    private readonly ObservableCollection<RadioStation> _stations = new();
    private CancellationTokenSource? _searchCts;
    private bool _initialized;

    public RadioStation? Selected { get; private set; }

    public RadioPickerWindow()
    {
        InitializeComponent();
        StationsGrid.ItemsSource = _stations;
        Loaded += async (_, _) => await InitAsync();
    }

    private async Task InitAsync()
    {
        StatusText.Text = L.RadioLoading;
        try
        {
            var countriesTask = _api.GetCountriesAsync();
            var tagsTask = _api.GetTopTagsAsync(120);
            await Task.WhenAll(countriesTask, tagsTask);

            var countries = await countriesTask;
            var tags = await tagsTask;

            countries.Insert(0, new RadioCountry { Name = L.AllOption, Iso = "" });
            tags.Insert(0, new RadioTag { Name = "" });

            CountryCombo.ItemsSource = countries;
            TagCombo.ItemsSource = tags;

            var defaultIso = GuessDefaultCountryIso();
            var defaultCountry = countries.FirstOrDefault(c =>
                string.Equals(c.Iso, defaultIso, StringComparison.OrdinalIgnoreCase));
            CountryCombo.SelectedItem = defaultCountry ?? countries[0];
            TagCombo.SelectedIndex = 0;

            _initialized = true;
            await SearchAsync();
        }
        catch (Exception ex)
        {
            StatusText.Text = L.RadioLoadError(ex.Message);
        }
    }

    private static string GuessDefaultCountryIso()
    {
        try
        {
            var region = new RegionInfo(CultureInfo.CurrentCulture.Name);
            return region.TwoLetterISORegionName;
        }
        catch
        {
            return "HU";
        }
    }

    private async Task SearchAsync()
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var ct = _searchCts.Token;

        string? country = null, tag = null;
        if (CountryCombo.SelectedItem is RadioCountry c && !string.IsNullOrEmpty(c.Iso))
            country = c.Name;
        if (TagCombo.SelectedItem is RadioTag t && !string.IsNullOrWhiteSpace(t.Name))
            tag = t.Name;
        var name = NameSearch.Text?.Trim();

        StatusText.Text = L.RadioSearching;
        OkButton.IsEnabled = false;

        try
        {
            var stations = await _api.SearchAsync(country, tag, name, 250, ct);
            _stations.Clear();
            foreach (var s in stations) _stations.Add(s);
            StatusText.Text = stations.Count == 0
                ? L.RadioNoResults
                : L.RadioResultsCount(stations.Count);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            StatusText.Text = L.RadioErrorPrefix(ex.Message);
        }
    }

    private async void Search_Click(object sender, RoutedEventArgs e) => await SearchAsync();

    private async void Filter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!_initialized) return;
        await SearchAsync();
    }

    private async void NameSearch_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) await SearchAsync();
    }

    private void StationsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var s = StationsGrid.SelectedItem as RadioStation;
        OkButton.IsEnabled = s is not null;
        SelectedUrlBox.Text = s?.PlayUrl ?? "";
    }

    private void StationsGrid_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (StationsGrid.SelectedItem is RadioStation) Ok_Click(sender, e);
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Selected = null;
        DialogResult = false;
        Close();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        Selected = StationsGrid.SelectedItem as RadioStation;
        if (Selected is null) return;
        DialogResult = true;
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        try { _searchCts?.Cancel(); } catch { }
        base.OnClosed(e);
    }
}
