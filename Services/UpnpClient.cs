using System.Net.Http;
using System.Text;
using System.Xml.Linq;
using BoseSoundTouchBridge.Localization;

namespace BoseSoundTouchBridge.Services;

public class UpnpClient
{
    private const string AvTransportServiceType = "urn:schemas-upnp-org:service:AVTransport:1";
    private const int UpnpPort = 8091;
    private const int SoundTouchApiPort = 8090;

    private readonly HttpClient _http;
    private readonly string _host;
    private Uri? _controlUrl;

    public UpnpClient(string host)
    {
        _host = host;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
    }

    public async Task PlayUrlAsync(string url, string title, CancellationToken ct = default)
    {
        await EnsureControlUrlAsync(ct);
        if (_controlUrl is null) throw new InvalidOperationException(L.UpnpControlUrlMissing);

        url = ForceHttp(url);
        var didl = BuildDidl(url, title);

        await SoapAsync("Stop",
            $"<InstanceID>0</InstanceID>", ct);

        await SoapAsync("SetAVTransportURI",
            $"<InstanceID>0</InstanceID>" +
            $"<CurrentURI>{XmlEscape(url)}</CurrentURI>" +
            $"<CurrentURIMetaData>{XmlEscape(didl)}</CurrentURIMetaData>", ct);

        await SoapAsync("Play",
            $"<InstanceID>0</InstanceID><Speed>1</Speed>", ct);
    }

    private async Task EnsureControlUrlAsync(CancellationToken ct)
    {
        if (_controlUrl is not null) return;

        var deviceId = await FetchDeviceIdAsync(ct);
        var descUrl = $"http://{_host}:{UpnpPort}/XD/BO5EBO5E-F00D-F00D-FEED-{deviceId}.xml";

        XDocument doc;
        try
        {
            using var resp = await _http.GetAsync(descUrl, ct);
            resp.EnsureSuccessStatusCode();
            var xml = await resp.Content.ReadAsStringAsync(ct);
            doc = XDocument.Parse(xml);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(L.UpnpDescUnavailable(descUrl, ex.Message), ex);
        }

        XNamespace ns = "urn:schemas-upnp-org:device-1-0";
        var service = doc.Descendants(ns + "service")
            .FirstOrDefault(s => (string?)s.Element(ns + "serviceType") == AvTransportServiceType);

        var controlPath = (string?)service?.Element(ns + "controlURL");
        if (string.IsNullOrWhiteSpace(controlPath))
            throw new InvalidOperationException(L.UpnpAvTransportNotFound);

        _controlUrl = new Uri(new Uri($"http://{_host}:{UpnpPort}/"), controlPath);
    }

    private async Task<string> FetchDeviceIdAsync(CancellationToken ct)
    {
        var infoUrl = $"http://{_host}:{SoundTouchApiPort}/info";
        try
        {
            using var resp = await _http.GetAsync(infoUrl, ct);
            resp.EnsureSuccessStatusCode();
            var xml = await resp.Content.ReadAsStringAsync(ct);
            var doc = XDocument.Parse(xml);
            var id = (string?)doc.Root?.Attribute("deviceID");
            if (string.IsNullOrWhiteSpace(id))
                throw new InvalidOperationException(L.SoundtouchInfoNoDeviceId);
            return id;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(L.SoundtouchInfoUnreachable(infoUrl, ex.Message), ex);
        }
    }

    private async Task SoapAsync(string action, string innerArgs, CancellationToken ct)
    {
        var soap =
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
            "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" " +
            "s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">" +
            "<s:Body>" +
            $"<u:{action} xmlns:u=\"{AvTransportServiceType}\">{innerArgs}</u:{action}>" +
            "</s:Body></s:Envelope>";

        using var req = new HttpRequestMessage(HttpMethod.Post, _controlUrl)
        {
            Content = new StringContent(soap, Encoding.UTF8, "text/xml")
        };
        req.Content.Headers.ContentType!.CharSet = "utf-8";
        req.Headers.Add("SOAPACTION", $"\"{AvTransportServiceType}#{action}\"");

        using var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(L.UpnpActionError(action, (int)resp.StatusCode, Trim(body, 400)));
        }
    }

    private static string BuildDidl(string url, string title)
    {
        var mime = InferMime(url);
        return
            "<DIDL-Lite xmlns=\"urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/\" " +
            "xmlns:dc=\"http://purl.org/dc/elements/1.1/\" " +
            "xmlns:upnp=\"urn:schemas-upnp-org:metadata-1-0/upnp/\">" +
            "<item id=\"0\" parentID=\"-1\" restricted=\"1\">" +
            $"<dc:title>{XmlEscape(title)}</dc:title>" +
            "<upnp:class>object.item.audioItem.audioBroadcast</upnp:class>" +
            $"<res protocolInfo=\"http-get:*:{mime}:*\">{XmlEscape(url)}</res>" +
            "</item></DIDL-Lite>";
    }

    private static string InferMime(string url)
    {
        var lower = url.ToLowerInvariant();
        if (lower.EndsWith(".mp3")) return "audio/mpeg";
        if (lower.EndsWith(".aac")) return "audio/aac";
        if (lower.EndsWith(".m4a")) return "audio/mp4";
        if (lower.EndsWith(".flac")) return "audio/flac";
        if (lower.EndsWith(".ogg") || lower.EndsWith(".oga")) return "audio/ogg";
        if (lower.EndsWith(".wav")) return "audio/wav";
        if (lower.EndsWith(".m3u") || lower.EndsWith(".m3u8")) return "audio/mpegurl";
        if (lower.EndsWith(".pls")) return "audio/x-scpls";
        return "audio/mpeg";
    }

    private static string ForceHttp(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return url;
        if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return "http://" + url.Substring(8);
        return url;
    }

    private static string XmlEscape(string s) => s
        .Replace("&", "&amp;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;")
        .Replace("\"", "&quot;")
        .Replace("'", "&apos;");

    private static string Trim(string s, int max) => s.Length <= max ? s : s.Substring(0, max) + "…";
}
