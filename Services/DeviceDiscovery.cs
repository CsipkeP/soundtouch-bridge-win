using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Xml.Linq;

namespace BoseSoundTouchBridge.Services;

public class DiscoveredDevice
{
    public string Ip { get; set; } = "";
    public string Name { get; set; } = "";
    public string Model { get; set; } = "";
    public string DeviceId { get; set; } = "";
    public override string ToString() =>
        string.IsNullOrWhiteSpace(Name) ? Ip : $"{Name} ({Ip})";
}

public class DeviceDiscovery
{
    private const string Multicast = "239.255.255.250";
    private const int SsdpPort = 1900;
    private const int SoundTouchApiPort = 8090;

    public event EventHandler<DiscoveredDevice>? DeviceFound;

    public async Task DiscoverAsync(TimeSpan timeout, CancellationToken ct = default)
    {
        var seenIps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var udp = new UdpClient();
        udp.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 2);
        udp.EnableBroadcast = true;

        var endpoint = new IPEndPoint(IPAddress.Parse(Multicast), SsdpPort);
        var requestBytes = Encoding.ASCII.GetBytes(BuildSearch("urn:schemas-upnp-org:device:MediaRenderer:1"));

        for (int i = 0; i < 3; i++)
        {
            await udp.SendAsync(requestBytes, requestBytes.Length, endpoint);
            await Task.Delay(150, ct);
        }

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(timeout);

        var verifyTasks = new List<Task>();

        try
        {
            while (!deadline.IsCancellationRequested)
            {
                UdpReceiveResult result;
                try { result = await udp.ReceiveAsync(deadline.Token); }
                catch (OperationCanceledException) { break; }

                var msg = Encoding.ASCII.GetString(result.Buffer);
                var location = ExtractHeader(msg, "LOCATION");
                if (string.IsNullOrEmpty(location)) continue;
                if (!Uri.TryCreate(location, UriKind.Absolute, out var locUri)) continue;

                var ip = locUri.Host;
                lock (seenIps) { if (!seenIps.Add(ip)) continue; }

                verifyTasks.Add(VerifyAndReportAsync(http, ip, ct));
            }
        }
        finally
        {
            try { await Task.WhenAll(verifyTasks).WaitAsync(TimeSpan.FromSeconds(3), ct); }
            catch { }
        }
    }

    private async Task VerifyAndReportAsync(HttpClient http, string ip, CancellationToken ct)
    {
        try
        {
            using var infoCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            infoCts.CancelAfter(TimeSpan.FromSeconds(2));
            using var resp = await http.GetAsync($"http://{ip}:{SoundTouchApiPort}/info", infoCts.Token);
            if (!resp.IsSuccessStatusCode) return;
            var xml = await resp.Content.ReadAsStringAsync(infoCts.Token);
            var doc = XDocument.Parse(xml);
            if (doc.Root is null || doc.Root.Name.LocalName != "info") return;

            var dev = new DiscoveredDevice
            {
                Ip = ip,
                Name = ((string?)doc.Root.Element("name") ?? "").Trim(),
                Model = ((string?)doc.Root.Element("type") ?? "").Trim(),
                DeviceId = ((string?)doc.Root.Attribute("deviceID") ?? "").Trim()
            };
            if (string.IsNullOrWhiteSpace(dev.Name)) dev.Name = "Bose SoundTouch";
            DeviceFound?.Invoke(this, dev);
        }
        catch { /* not a SoundTouch */ }
    }

    private static string BuildSearch(string st) =>
        "M-SEARCH * HTTP/1.1\r\n" +
        $"HOST: {Multicast}:{SsdpPort}\r\n" +
        "MAN: \"ssdp:discover\"\r\n" +
        "MX: 2\r\n" +
        $"ST: {st}\r\n" +
        "USER-AGENT: BoseSoundTouchBridge/1.0\r\n" +
        "\r\n";

    private static string? ExtractHeader(string msg, string name)
    {
        foreach (var rawLine in msg.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            var idx = line.IndexOf(':');
            if (idx < 0) continue;
            if (string.Equals(line[..idx].Trim(), name, StringComparison.OrdinalIgnoreCase))
                return line[(idx + 1)..].Trim();
        }
        return null;
    }
}
