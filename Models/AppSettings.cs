namespace BoseSoundTouchBridge.Models;

public class AppSettings
{
    public string IpAddress { get; set; } = "";
    public string Language { get; set; } = "";
    public List<Preset> Presets { get; set; } = CreateDefaultPresets();
    public SpotifyConfig Spotify { get; set; } = new();

    public static AppSettings CreateDefault() => new();

    private static List<Preset> CreateDefaultPresets()
    {
        var list = new List<Preset>();
        for (int i = 1; i <= 6; i++)
        {
            list.Add(new Preset { Name = $"Preset {i}", Url = "" });
        }
        return list;
    }

    public void EnsureSixPresets()
    {
        while (Presets.Count < 6)
        {
            Presets.Add(new Preset { Name = $"Preset {Presets.Count + 1}", Url = "" });
        }
        if (Presets.Count > 6) Presets = Presets.Take(6).ToList();
    }
}

public class Preset
{
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";
}

public class SpotifyConfig
{
    public string ClientId { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public string UserName { get; set; } = "";
    public string DeviceId { get; set; } = "";
    public string DeviceName { get; set; } = "";

    public bool IsConnected =>
        !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(RefreshToken);

    public bool CanPlay =>
        IsConnected && !string.IsNullOrWhiteSpace(DeviceId);
}
