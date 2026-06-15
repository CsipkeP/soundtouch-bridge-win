using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using BoseSoundTouchBridge.Localization;
using BoseSoundTouchBridge.Models;

namespace BoseSoundTouchBridge.Services;

public class SpotifyDevice
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("is_active")] public bool IsActive { get; set; }
    [JsonPropertyName("is_restricted")] public bool IsRestricted { get; set; }
    [JsonPropertyName("volume_percent")] public int? VolumePercent { get; set; }
    public string Display => $"{Name} — {Type}" + (IsActive ? "  ●" : "");
}

public class SpotifyApi
{
    private const string AuthBase = "https://accounts.spotify.com";
    private const string ApiBase = "https://api.spotify.com";
    public const string RedirectUri = "http://127.0.0.1:38765/callback";
    private const string Scopes = "user-read-playback-state user-modify-playback-state user-read-email";

    private readonly Func<SpotifyConfig> _getConfig;
    private readonly Action<SpotifyConfig> _saveConfig;
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    private string? _accessToken;
    private DateTime _accessExpiry = DateTime.MinValue;

    public SpotifyApi(Func<SpotifyConfig> getConfig, Action<SpotifyConfig> saveConfig)
    {
        _getConfig = getConfig;
        _saveConfig = saveConfig;
    }

    public async Task<(string RefreshToken, string UserName)> AuthorizeAsync(
        string clientId, CancellationToken ct = default)
    {
        var verifier = GeneratePkceVerifier();
        var challenge = ComputePkceChallenge(verifier);

        using var listener = new HttpListener();
        listener.Prefixes.Add("http://127.0.0.1:38765/");
        try { listener.Start(); }
        catch (HttpListenerException ex)
        {
            throw new InvalidOperationException(L.SpotifyRedirectPortError(ex.Message), ex);
        }

        var authUrl = $"{AuthBase}/authorize?" +
            $"response_type=code&client_id={Uri.EscapeDataString(clientId)}&" +
            $"redirect_uri={Uri.EscapeDataString(RedirectUri)}&" +
            $"code_challenge_method=S256&code_challenge={challenge}&" +
            $"scope={Uri.EscapeDataString(Scopes)}";

        try
        {
            Process.Start(new ProcessStartInfo { FileName = authUrl, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            listener.Stop();
            throw new InvalidOperationException(L.SpotifyBrowserError(ex.Message), ex);
        }

        var contextTask = listener.GetContextAsync();
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromMinutes(5));

        var completed = await Task.WhenAny(contextTask, Task.Delay(Timeout.Infinite, timeoutCts.Token));
        if (completed != contextTask)
        {
            listener.Stop();
            throw new TimeoutException(L.SpotifyLoginTimeout);
        }

        var context = await contextTask;
        var code = context.Request.QueryString["code"];
        var error = context.Request.QueryString["error"];

        var html = error is null
            ? "<!doctype html><html><head><meta charset='utf-8'><title>Spotify</title></head>" +
              "<body style='font-family:sans-serif;text-align:center;padding:60px;background:#191414;color:#fff'>" +
              $"<h2 style='color:#1DB954'>✓ {WebUtility.HtmlEncode(L.SpotifyCallbackOk)}</h2>" +
              $"<p>{WebUtility.HtmlEncode(L.SpotifyCallbackOkSub)}</p></body></html>"
            : $"<!doctype html><html><head><meta charset='utf-8'></head>" +
              $"<body style='font-family:sans-serif;text-align:center;padding:60px'>" +
              $"<h2 style='color:#c00'>{WebUtility.HtmlEncode(L.SpotifyCallbackErr)}</h2>" +
              $"<p>{WebUtility.HtmlEncode(error)}</p></body></html>";
        var bytes = Encoding.UTF8.GetBytes(html);
        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.ContentLength64 = bytes.Length;
        await context.Response.OutputStream.WriteAsync(bytes, ct);
        context.Response.Close();
        listener.Stop();

        if (error is not null) throw new InvalidOperationException(L.SpotifyAuthError(error));
        if (string.IsNullOrEmpty(code)) throw new InvalidOperationException(L.SpotifyNoAuthCode);

        var tokenForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = RedirectUri,
            ["client_id"] = clientId,
            ["code_verifier"] = verifier
        });
        using var tokenResp = await _http.PostAsync($"{AuthBase}/api/token", tokenForm, ct);
        if (!tokenResp.IsSuccessStatusCode)
        {
            var body = await tokenResp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(L.SpotifyTokenExchangeError((int)tokenResp.StatusCode, body));
        }
        var tokens = await tokenResp.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct);
        if (tokens is null || string.IsNullOrEmpty(tokens.AccessToken) || string.IsNullOrEmpty(tokens.RefreshToken))
            throw new InvalidOperationException(L.SpotifyTokenIncomplete);

        _accessToken = tokens.AccessToken;
        _accessExpiry = DateTime.UtcNow.AddSeconds(tokens.ExpiresIn - 60);

        var meReq = new HttpRequestMessage(HttpMethod.Get, $"{ApiBase}/v1/me");
        meReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        using var meResp = await _http.SendAsync(meReq, ct);
        meResp.EnsureSuccessStatusCode();
        var me = await meResp.Content.ReadFromJsonAsync<MeResponse>(cancellationToken: ct);
        var name = me?.DisplayName ?? me?.Email ?? me?.Id ?? "Spotify user";

        return (tokens.RefreshToken, name);
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken ct = default)
    {
        if (_accessToken is not null && DateTime.UtcNow < _accessExpiry)
            return _accessToken;

        await _tokenLock.WaitAsync(ct);
        try
        {
            if (_accessToken is not null && DateTime.UtcNow < _accessExpiry)
                return _accessToken;

            var cfg = _getConfig();
            if (string.IsNullOrEmpty(cfg.RefreshToken) || string.IsNullOrEmpty(cfg.ClientId))
                throw new InvalidOperationException(L.SpotifyNotConnectedErr);

            var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = cfg.RefreshToken,
                ["client_id"] = cfg.ClientId
            });
            using var resp = await _http.PostAsync($"{AuthBase}/api/token", form, ct);
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(ct);
                throw new InvalidOperationException(L.SpotifyRefreshError((int)resp.StatusCode, body));
            }
            var tokens = await resp.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct);
            if (tokens?.AccessToken is null)
                throw new InvalidOperationException(L.SpotifyTokenIncomplete);

            _accessToken = tokens.AccessToken;
            _accessExpiry = DateTime.UtcNow.AddSeconds(tokens.ExpiresIn - 60);

            if (!string.IsNullOrEmpty(tokens.RefreshToken) && tokens.RefreshToken != cfg.RefreshToken)
            {
                cfg.RefreshToken = tokens.RefreshToken;
                _saveConfig(cfg);
            }

            return _accessToken;
        }
        finally { _tokenLock.Release(); }
    }

    public async Task<List<SpotifyDevice>> GetDevicesAsync(CancellationToken ct = default)
    {
        var token = await GetAccessTokenAsync(ct);
        var req = new HttpRequestMessage(HttpMethod.Get, $"{ApiBase}/v1/me/player/devices");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        var data = await resp.Content.ReadFromJsonAsync<DevicesResponse>(cancellationToken: ct);
        return data?.Devices ?? new List<SpotifyDevice>();
    }

    public async Task PlayAsync(string deviceId, string contextUri, CancellationToken ct = default)
    {
        await DoPlayAsync(deviceId, contextUri, ct);
    }

    private async Task DoPlayAsync(string deviceId, string contextUri, CancellationToken ct)
    {
        var token = await GetAccessTokenAsync(ct);

        var body = IsTrackUri(contextUri)
            ? JsonSerializer.Serialize(new { uris = new[] { contextUri } })
            : JsonSerializer.Serialize(new { context_uri = contextUri });

        async Task<HttpResponseMessage> Send()
        {
            var url = $"{ApiBase}/v1/me/player/play?device_id={Uri.EscapeDataString(deviceId)}";
            var req = new HttpRequestMessage(HttpMethod.Put, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Content = new StringContent(body, Encoding.UTF8, "application/json");
            return await _http.SendAsync(req, ct);
        }

        using var first = await Send();
        if (first.IsSuccessStatusCode) return;

        if (first.StatusCode == HttpStatusCode.NotFound || first.StatusCode == HttpStatusCode.Forbidden)
        {
            try { await TransferPlaybackAsync(deviceId, true, ct); } catch { }
            await Task.Delay(900, ct);
            using var retry = await Send();
            if (retry.IsSuccessStatusCode) return;
            var retryBody = await retry.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(L.SpotifyPlayError((int)retry.StatusCode, Truncate(retryBody, 300)));
        }

        var firstBody = await first.Content.ReadAsStringAsync(ct);
        throw new InvalidOperationException(L.SpotifyPlayError((int)first.StatusCode, Truncate(firstBody, 300)));
    }

    public async Task TransferPlaybackAsync(string deviceId, bool play, CancellationToken ct = default)
    {
        var token = await GetAccessTokenAsync(ct);
        var req = new HttpRequestMessage(HttpMethod.Put, $"{ApiBase}/v1/me/player");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var body = JsonSerializer.Serialize(new { device_ids = new[] { deviceId }, play });
        req.Content = new StringContent(body, Encoding.UTF8, "application/json");
        using var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode && resp.StatusCode != HttpStatusCode.NoContent)
        {
            var respBody = await resp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(L.SpotifyTransferError((int)resp.StatusCode, Truncate(respBody, 300)));
        }
    }

    public static string? ParseUri(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        if (url.StartsWith("spotify:", StringComparison.OrdinalIgnoreCase)) return url.Trim();
        var m = Regex.Match(url,
            @"^https?://open\.spotify\.com/(?:intl-[a-z]+/)?(playlist|track|album|artist|show|episode)/([a-zA-Z0-9]+)",
            RegexOptions.IgnoreCase);
        if (m.Success) return $"spotify:{m.Groups[1].Value.ToLowerInvariant()}:{m.Groups[2].Value}";
        return null;
    }

    private static bool IsTrackUri(string uri) =>
        uri.StartsWith("spotify:track:", StringComparison.OrdinalIgnoreCase) ||
        uri.StartsWith("spotify:episode:", StringComparison.OrdinalIgnoreCase);

    private static string GeneratePkceVerifier()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string ComputePkceChallenge(string verifier)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(verifier));
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string Truncate(string s, int n) => s.Length <= n ? s : s[..n] + "…";

    private class TokenResponse
    {
        [JsonPropertyName("access_token")] public string AccessToken { get; set; } = "";
        [JsonPropertyName("token_type")] public string TokenType { get; set; } = "";
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
        [JsonPropertyName("refresh_token")] public string? RefreshToken { get; set; }
        [JsonPropertyName("scope")] public string Scope { get; set; } = "";
    }

    private class MeResponse
    {
        [JsonPropertyName("id")] public string Id { get; set; } = "";
        [JsonPropertyName("display_name")] public string? DisplayName { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
    }

    private class DevicesResponse
    {
        [JsonPropertyName("devices")] public List<SpotifyDevice> Devices { get; set; } = new();
    }
}
