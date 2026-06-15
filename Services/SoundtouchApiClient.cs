using System.Net.Http;
using System.Text;
using System.Xml.Linq;

namespace BoseSoundTouchBridge.Services;

public record DeviceVolume(int Level, bool Muted);

public sealed class SoundtouchApiClient : IDisposable
{
    private const int Port = 8090;
    private const string Sender = "Gabbo";

    private readonly HttpClient _http;
    public string Host { get; }

    public SoundtouchApiClient(string host)
    {
        Host = host;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
    }

    public Task TogglePowerAsync(CancellationToken ct = default) => PressKeyAsync("POWER", ct);
    public Task ToggleMuteAsync(CancellationToken ct = default) => PressKeyAsync("MUTE", ct);
    public Task VolumeUpAsync(CancellationToken ct = default) => PressKeyAsync("VOLUME_UP", ct);
    public Task VolumeDownAsync(CancellationToken ct = default) => PressKeyAsync("VOLUME_DOWN", ct);

    public async Task PressKeyAsync(string key, CancellationToken ct = default)
    {
        await SendKeyAsync(key, "press", ct);
        await SendKeyAsync(key, "release", ct);
    }

    private async Task SendKeyAsync(string key, string state, CancellationToken ct)
    {
        var body = $"<key state=\"{state}\" sender=\"{Sender}\">{key}</key>";
        using var content = new StringContent(body, Encoding.UTF8, "application/xml");
        using var resp = await _http.PostAsync($"http://{Host}:{Port}/key", content, ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task<DeviceVolume> GetVolumeAsync(CancellationToken ct = default)
    {
        using var resp = await _http.GetAsync($"http://{Host}:{Port}/volume", ct);
        resp.EnsureSuccessStatusCode();
        var xml = await resp.Content.ReadAsStringAsync(ct);
        var doc = XDocument.Parse(xml);
        var actual = (int?)doc.Root?.Element("actualvolume") ?? 0;
        var mutedRaw = (string?)doc.Root?.Element("muteenabled");
        var muted = string.Equals(mutedRaw, "true", StringComparison.OrdinalIgnoreCase);
        return new DeviceVolume(actual, muted);
    }

    public async Task SetVolumeAsync(int volume, CancellationToken ct = default)
    {
        var v = Math.Clamp(volume, 0, 100);
        var body = $"<volume>{v}</volume>";
        using var content = new StringContent(body, Encoding.UTF8, "application/xml");
        using var resp = await _http.PostAsync($"http://{Host}:{Port}/volume", content, ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task<string> GetSourceAsync(CancellationToken ct = default)
    {
        using var resp = await _http.GetAsync($"http://{Host}:{Port}/now_playing", ct);
        resp.EnsureSuccessStatusCode();
        var xml = await resp.Content.ReadAsStringAsync(ct);
        var doc = XDocument.Parse(xml);
        return (string?)doc.Root?.Attribute("source") ?? "UNKNOWN";
    }

    public async Task<bool> IsPoweredOnAsync(CancellationToken ct = default)
    {
        var source = await GetSourceAsync(ct);
        return !string.Equals(source, "STANDBY", StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose() => _http.Dispose();
}
