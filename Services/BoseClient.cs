using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using BoseSoundTouchBridge.Localization;
using BoseSoundTouchBridge.Models;

namespace BoseSoundTouchBridge.Services;

public enum BridgeState { Disconnected, Connecting, Connected, Error }

public class BridgeStatusEventArgs : EventArgs
{
    public BridgeState State { get; }
    public string Message { get; }
    public BridgeStatusEventArgs(BridgeState state, string message)
    {
        State = state;
        Message = message;
    }
}

public class PresetTriggeredEventArgs : EventArgs
{
    public int PresetId { get; }
    public Preset Preset { get; }
    public bool Success { get; }
    public string? Error { get; }
    public PresetTriggeredEventArgs(int id, Preset preset, bool success, string? error)
    {
        PresetId = id;
        Preset = preset;
        Success = success;
        Error = error;
    }
}

public sealed class BoseClient : IDisposable
{
    private static readonly Regex PresetRegex = new(
        @"<nowSelectionUpdated[^>]*>.*?<preset\s+id=""([1-6])""",
        RegexOptions.Singleline | RegexOptions.Compiled);

    private CancellationTokenSource? _cts;
    private Task? _runLoop;
    private AppSettings _settings;
    private readonly SpotifyApi _spotify;
    private UpnpClient? _upnp;
    private string? _activeHost;

    public BridgeState State { get; private set; } = BridgeState.Disconnected;
    public event EventHandler<BridgeStatusEventArgs>? StatusChanged;
    public event EventHandler<PresetTriggeredEventArgs>? PresetTriggered;

    public BoseClient(AppSettings settings, SpotifyApi spotify)
    {
        _settings = settings;
        _spotify = spotify;
    }

    public void UpdateSettings(AppSettings settings)
    {
        var hostChanged = !string.Equals(settings.IpAddress, _settings.IpAddress, StringComparison.OrdinalIgnoreCase);
        _settings = settings;
        if (hostChanged) Restart();
    }

    public void Start()
    {
        if (_runLoop is not null && !_runLoop.IsCompleted) return;
        if (string.IsNullOrWhiteSpace(_settings.IpAddress))
        {
            Report(BridgeState.Disconnected, L.BridgeIpEmpty);
            return;
        }

        _activeHost = _settings.IpAddress.Trim();
        _upnp = new UpnpClient(_activeHost);
        _cts = new CancellationTokenSource();
        _runLoop = Task.Run(() => RunLoopAsync(_cts.Token));
    }

    public void Stop()
    {
        try { _cts?.Cancel(); } catch { }
        try { _runLoop?.Wait(TimeSpan.FromSeconds(2)); } catch { }
        _cts?.Dispose();
        _cts = null;
        _runLoop = null;
        Report(BridgeState.Disconnected, L.StatusStopped);
    }

    public void Restart()
    {
        Stop();
        Start();
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        var delaySec = 2;
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ConnectAndPumpAsync(ct);
                delaySec = 2;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Report(BridgeState.Error, L.BridgeConnectError(ex.Message));
            }

            if (ct.IsCancellationRequested) break;
            try { await Task.Delay(TimeSpan.FromSeconds(delaySec), ct); } catch { }
            delaySec = Math.Min(delaySec * 2, 30);
        }
    }

    private async Task ConnectAndPumpAsync(CancellationToken ct)
    {
        if (_activeHost is null) return;

        using var ws = new ClientWebSocket();
        ws.Options.AddSubProtocol("gabbo");
        ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);

        var uri = new Uri($"ws://{_activeHost}:8080");
        Report(BridgeState.Connecting, L.BridgeConnectingTo(uri));

        await ws.ConnectAsync(uri, ct);
        Report(BridgeState.Connected, L.BridgeConnectedTo(_activeHost));

        var buffer = new byte[16 * 1024];
        var sb = new StringBuilder();

        while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            sb.Clear();
            WebSocketReceiveResult result;
            do
            {
                result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
                    return;
                }
                sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            } while (!result.EndOfMessage);

            var msg = sb.ToString();
            await HandleMessageAsync(msg, ct);
        }
    }

    private async Task HandleMessageAsync(string message, CancellationToken ct)
    {
        var m = PresetRegex.Match(message);
        if (!m.Success) return;
        if (!int.TryParse(m.Groups[1].Value, out var presetId)) return;
        if (presetId < 1 || presetId > 6) return;

        var preset = _settings.Presets[presetId - 1];
        if (string.IsNullOrWhiteSpace(preset.Url))
        {
            PresetTriggered?.Invoke(this,
                new PresetTriggeredEventArgs(presetId, preset, false, L.HintPresetNoUrl(presetId)));
            return;
        }

        try
        {
            var title = string.IsNullOrWhiteSpace(preset.Name) ? $"Preset {presetId}" : preset.Name;
            var spotifyUri = SpotifyApi.ParseUri(preset.Url);
            if (spotifyUri is not null)
            {
                var sp = _settings.Spotify;
                if (string.IsNullOrEmpty(sp.DeviceId))
                    throw new InvalidOperationException(L.SpotifyDevicePresetMissing);
                await _spotify.PlayAsync(sp.DeviceId, spotifyUri, ct);
            }
            else
            {
                if (_upnp is null) throw new InvalidOperationException(L.UpnpClientNotInit);
                await _upnp.PlayUrlAsync(preset.Url, title, ct);
            }
            PresetTriggered?.Invoke(this, new PresetTriggeredEventArgs(presetId, preset, true, null));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            PresetTriggered?.Invoke(this,
                new PresetTriggeredEventArgs(presetId, preset, false, ex.Message));
        }
    }

    private void Report(BridgeState state, string message)
    {
        State = state;
        StatusChanged?.Invoke(this, new BridgeStatusEventArgs(state, message));
    }

    public void Dispose() => Stop();
}
