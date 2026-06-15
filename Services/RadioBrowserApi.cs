using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using BoseSoundTouchBridge.Localization;

namespace BoseSoundTouchBridge.Services;

public class RadioStation
{
    [JsonPropertyName("stationuuid")] public string StationUuid { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("url")] public string Url { get; set; } = "";
    [JsonPropertyName("url_resolved")] public string UrlResolved { get; set; } = "";
    [JsonPropertyName("homepage")] public string Homepage { get; set; } = "";
    [JsonPropertyName("favicon")] public string Favicon { get; set; } = "";
    [JsonPropertyName("tags")] public string Tags { get; set; } = "";
    [JsonPropertyName("country")] public string Country { get; set; } = "";
    [JsonPropertyName("countrycode")] public string CountryCode { get; set; } = "";
    [JsonPropertyName("language")] public string Language { get; set; } = "";
    [JsonPropertyName("votes")] public int Votes { get; set; }
    [JsonPropertyName("codec")] public string Codec { get; set; } = "";
    [JsonPropertyName("bitrate")] public int Bitrate { get; set; }
    [JsonPropertyName("clickcount")] public int ClickCount { get; set; }

    public string PlayUrl
    {
        get
        {
            var u = string.IsNullOrWhiteSpace(UrlResolved) ? Url : UrlResolved;
            return ForceHttp(u);
        }
    }

    private static string ForceHttp(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return url;
        if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return "http://" + url.Substring(8);
        return url;
    }
}

public class RadioCountry
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("iso_3166_1")] public string Iso { get; set; } = "";
    [JsonPropertyName("stationcount")] public int StationCount { get; set; }
    public string Display => string.IsNullOrEmpty(Iso) ? Name : $"{Name} ({StationCount})";
}

public class RadioTag
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("stationcount")] public int StationCount { get; set; }
    public string Display => string.IsNullOrEmpty(Name) ? "(Mind)" : $"{Name} ({StationCount})";
}

public class RadioBrowserApi
{
    private const string PrimaryBase = "https://all.api.radio-browser.info";
    private static readonly string[] Fallbacks =
    {
        "https://de1.api.radio-browser.info",
        "https://de2.api.radio-browser.info",
        "https://at1.api.radio-browser.info",
        "https://nl1.api.radio-browser.info",
        "https://fr1.api.radio-browser.info"
    };

    private static readonly HttpClient _http;
    private static string? _stickyBase;

    static RadioBrowserApi()
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("BoseSoundTouchBridge/1.0");
    }

    private async Task<HttpResponseMessage> GetAsync(string path, CancellationToken ct)
    {
        var bases = new List<string>();
        if (_stickyBase is not null) bases.Add(_stickyBase);
        if (!bases.Contains(PrimaryBase)) bases.Add(PrimaryBase);
        foreach (var fb in Fallbacks) if (!bases.Contains(fb)) bases.Add(fb);

        Exception? lastEx = null;
        foreach (var baseUrl in bases)
        {
            try
            {
                using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                attemptCts.CancelAfter(TimeSpan.FromSeconds(8));
                var resp = await _http.GetAsync($"{baseUrl}{path}", attemptCts.Token);
                if (resp.IsSuccessStatusCode)
                {
                    _stickyBase = baseUrl;
                    return resp;
                }
                resp.Dispose();
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex) { lastEx = ex; }
        }
        throw new InvalidOperationException(L.RadioBrowserNoServer, lastEx);
    }

    public async Task<List<RadioCountry>> GetCountriesAsync(CancellationToken ct = default)
    {
        using var resp = await GetAsync("/json/countries?order=name&hidebroken=true", ct);
        var data = await resp.Content.ReadFromJsonAsync<List<RadioCountry>>(cancellationToken: ct);
        return data ?? new List<RadioCountry>();
    }

    public async Task<List<RadioTag>> GetTopTagsAsync(int limit, CancellationToken ct = default)
    {
        using var resp = await GetAsync(
            $"/json/tags?order=stationcount&reverse=true&hidebroken=true&limit={limit}", ct);
        var data = await resp.Content.ReadFromJsonAsync<List<RadioTag>>(cancellationToken: ct);
        return data ?? new List<RadioTag>();
    }

    public async Task<List<RadioStation>> SearchAsync(
        string? country, string? tag, string? name, int limit,
        CancellationToken ct = default)
    {
        var qs = new List<string> { "hidebroken=true", "order=clickcount", "reverse=true", $"limit={limit}" };
        if (!string.IsNullOrWhiteSpace(country)) qs.Add($"country={Uri.EscapeDataString(country)}");
        if (!string.IsNullOrWhiteSpace(tag)) qs.Add($"tag={Uri.EscapeDataString(tag)}");
        if (!string.IsNullOrWhiteSpace(name)) qs.Add($"name={Uri.EscapeDataString(name)}");
        var path = $"/json/stations/search?{string.Join('&', qs)}";
        using var resp = await GetAsync(path, ct);
        var data = await resp.Content.ReadFromJsonAsync<List<RadioStation>>(cancellationToken: ct);
        return data ?? new List<RadioStation>();
    }
}
