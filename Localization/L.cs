using System.Globalization;

namespace BoseSoundTouchBridge.Localization;

public static class L
{
    public static string Lang { get; private set; } = "hu";

    public static void Initialize(string lang) =>
        Lang = string.Equals(lang, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "hu";

    public static string DetectDefault()
    {
        try
        {
            return CultureInfo.CurrentUICulture.Name.StartsWith("hu", StringComparison.OrdinalIgnoreCase)
                ? "hu" : "en";
        }
        catch { return "hu"; }
    }

    private static string T(string hu, string en) => Lang == "hu" ? hu : en;
    private static string F(string huFmt, string enFmt, params object[] args) =>
        string.Format(Lang == "hu" ? huFmt : enFmt, args);

    // ---------- App / tray ----------
    public static string AppDuplicate => T(
        "A Bose SoundTouch Bridge már fut.",
        "Bose SoundTouch Bridge is already running.");
    public static string AppErrorTitle => T("Bose SoundTouch Bridge — hiba", "Bose SoundTouch Bridge — error");
    public static string TraySettings => T("Beállítások…", "Settings…");
    public static string TraySpotify => T("Spotify Connect…", "Spotify Connect…");
    public static string TrayReconnect => T("Újrakapcsolódás", "Reconnect");
    public static string TrayHelp => T("Súgó…", "Help…");
    public static string TrayExit => T("Kilépés", "Exit");
    public static string TrayTooltip(string msg) => F("Bose SoundTouch Bridge — {0}", "Bose SoundTouch Bridge — {0}", msg);

    // ---------- Main window ----------
    public static string MainTitle => T(
        "Bose SoundTouch Bridge — Beállítások",
        "Bose SoundTouch Bridge — Settings");
    public static string IpAddressLabel => T("SoundTouch IP cím:", "SoundTouch IP address:");
    public static string SearchDeviceTooltip => T("Eszköz keresése a hálózaton", "Find device on the network");
    public static string StatusWaiting => T("Várakozás…", "Waiting…");
    public static string ReconnectTooltip => T("Újrakapcsolódás (websocket)", "Reconnect (websocket)");
    public static string SpotifyConnectButton => T("Spotify Connect…", "Spotify Connect…");
    public static string HelpTooltip => T("Súgó / Használati útmutató", "Help / User guide");
    public static string LangToggleTooltip => T("Nyelv váltása (Switch language)", "Switch language (Nyelv váltása)");

    public static string PowerTooltip => T("Be / Ki", "Power on / off");
    public static string MuteTooltip => T("Némítás", "Mute");
    public static string MuteOffTooltip => T("Némítás kikapcsolása", "Unmute");
    public static string VolumeDownTooltip => T("Hangerő csökkentése", "Volume down");
    public static string VolumeUpTooltip => T("Hangerő növelése", "Volume up");
    public static string RefreshStateTooltip => T("Állapot frissítése", "Refresh state");
    public static string PresetTestTooltip => T("Lejátszás (preset tesztelése)", "Play (test preset)");
    public static string RadioBrowseTooltip => T("Rádió kereső", "Radio browser");

    public static string CloseButton => T("Bezárás", "Close");
    public static string SaveAndReconnectButton => T("Mentés és újrakapcsolódás", "Save and reconnect");

    public static string StatusConnected => T("Csatlakoztatva.", "Connected.");
    public static string StatusConnecting => T("Csatlakozás…", "Connecting…");
    public static string StatusErrorRetry => T("Hiba — újrapróbálkozás folyamatban.", "Error — retrying.");
    public static string StatusStopped => T("Leállítva.", "Stopped.");

    // Bridge status events
    public static string BridgeIpEmpty => T("IP cím nincs beállítva.", "IP address is not set.");
    public static string BridgeConnectError(string err) => F("Kapcsolódási hiba: {0}", "Connection error: {0}", err);
    public static string BridgeConnectingTo(Uri uri) => F("Kapcsolódás: {0}", "Connecting: {0}", uri);
    public static string BridgeConnectedTo(string host) => F("Csatlakozva: {0}", "Connected: {0}", host);

    // Hints / errors
    public static string HintGiveIp => T("Adj meg egy IP címet.", "Enter an IP address.");
    public static string HintGiveIpFirst => T("Először add meg az IP címet.", "Enter the IP address first.");
    public static string HintInvalidIp => T(
        "Adj meg érvényes IP címet vagy hostnevet.",
        "Enter a valid IP address or hostname.");
    public static string HintPresetNoUrl(int n) => F(
        "Preset {0}: nincs URL megadva.", "Preset {0}: no URL configured.", n);
    public static string HintTestingPreset(int n) => F(
        "Tesztelés: Preset {0} lejátszása…", "Testing: playing preset {0}…", n);
    public static string HintPlayStarted(int n) => F(
        "✓ Lejátszás indítva: Preset {0}", "✓ Playback started: preset {0}", n);
    public static string HintError(string err) => F("✗ Hiba: {0}", "✗ Error: {0}", err);
    public static string HintSaved(string path) => F("Mentve: {0}", "Saved: {0}", path);
    public static string HintReconnectStarted => T("Újrakapcsolódás indítva…", "Reconnect started…");
    public static string HintSelected(string name, string ip) => F(
        "Kiválasztva: {0} ({1})", "Selected: {0} ({1})", name, ip);
    public static string HintPresetSet(int n, string name) => F(
        "Preset {0} beállítva: {1}", "Preset {0} set: {1}", n, name);

    public static string HintPowerSent => T("Be/Ki gomb elküldve.", "Power command sent.");
    public static string HintMuteToggled => T("Némítás kapcsolva.", "Mute toggled.");
    public static string HintPowerError(string err) => F("Power hiba: {0}", "Power error: {0}", err);
    public static string HintMuteError(string err) => F("Mute hiba: {0}", "Mute error: {0}", err);
    public static string HintVolumeError(string err) => F("Hangerő hiba: {0}", "Volume error: {0}", err);
    public static string HintStateError(string err) => F(
        "Állapot lekérdezés hiba: {0}", "State query error: {0}", err);
    public static string HintPoweredOn(string source) => F(
        "Bekapcsolva — {0}", "Powered on — {0}", source);
    public static string HintPoweredOff => T(
        "Kikapcsolva — kattints a bekapcsoláshoz",
        "Powered off — click to turn on");

    public static string HintSpotifyNoDevice => T(
        "Spotify URI, de nincs Spotify eszköz kiválasztva (tray → Spotify Connect…).",
        "Spotify URI, but no Spotify device selected (tray → Spotify Connect…).");

    // Notifications
    public static string NotifyPlaying(string name) => F("Lejátszás: {0}", "Playing: {0}", name);
    public static string NotifyError(string name, string err) => F(
        "Hiba ({0}): {1}", "Error ({0}): {1}", name, err);

    // Restart prompt
    public static string LangChangeRestartTitle => T("Nyelv váltása", "Switch language");
    public static string LangChangeRestartText => T(
        "A nyelv váltásához az alkalmazás újraindul. Folytatod?",
        "The application will restart to switch language. Continue?");

    // ---------- Discovery window ----------
    public static string DiscoveryTitle => T("Eszköz keresése", "Find device");
    public static string DiscoverySearching => T("Keresés…", "Searching…");
    public static string DiscoveryError(string err) => F(
        "Hiba a kereséskor: {0}", "Search error: {0}", err);
    public static string DiscoveryNoneFound => T(
        "Nem található eszköz. Próbáld újrakeresni vagy add meg kézzel az IP-t.",
        "No devices found. Try again or enter the IP manually.");
    public static string DiscoveryFound(int n) => F(
        "{0} eszköz található.", "{0} device(s) found.", n);
    public static string DiscoveryFoundShort(int n) => F(
        "{0} eszköz található", "{0} device(s) found", n);
    public static string DiscoveryResearchButton => T("Újrakeresés", "Search again");
    public static string DiscoveryCancelButton => T("Mégse", "Cancel");
    public static string DiscoverySelectButton => T("Kiválasztás", "Select");

    // ---------- Radio picker ----------
    public static string RadioPickerTitle => T(
        "Internetes rádió kereső", "Internet Radio Browser");
    public static string RadioPickerHeader => T(
        "Válassz egy internetrádió-állomást", "Choose an internet radio station");
    public static string CountryLabel => T("Ország:", "Country:");
    public static string GenreLabel => T("Stílus:", "Genre:");
    public static string NameLabel => T("Név:", "Name:");
    public static string SearchButton => T("Keresés", "Search");
    public static string UrlLabel => T("URL:", "URL:");
    public static string ColName => T("Név", "Name");
    public static string ColCountry => T("Ország", "Country");
    public static string ColTags => T("Stílus", "Genre");
    public static string ColLanguage => T("Nyelv", "Language");
    public static string ColCodec => T("Codec", "Codec");
    public static string ColBitrate => T("Bitráta", "Bitrate");
    public static string ColClicks => T("Click", "Clicks");
    public static string AllOption => T("(Mind)", "(All)");
    public static string RadioLoading => T("Adatok betöltése…", "Loading…");
    public static string RadioLoadError(string err) => F(
        "Hiba a betöltéskor: {0}", "Load error: {0}", err);
    public static string RadioSearching => T("Keresés…", "Searching…");
    public static string RadioNoResults => T(
        "Nincs találat. Próbálj más szűrőt.",
        "No results. Try a different filter.");
    public static string RadioResultsCount(int n) => F(
        "{0} állomás található.", "{0} station(s) found.", n);
    public static string RadioErrorPrefix(string err) => F("Hiba: {0}", "Error: {0}", err);

    // ---------- Spotify settings ----------
    public static string SpotifyTitle => T(
        "Spotify Connect beállítások", "Spotify Connect Settings");
    public static string SpotifyHeader => T("Spotify Connect", "Spotify Connect");
    public static string SpotifyInstrStep1Pre => T("1. Nyisd meg a ", "1. Open the ");
    public static string SpotifyInstrStep1Post => T(
        " oldalt és hozz létre egy „app”-ot (pl. névnek: 'SoundTouch Bridge').",
        " page and create a new app (e.g. name: 'SoundTouch Bridge').");
    public static string SpotifyInstrStep2Pre => T(
        "2. A Redirect URI mezőhöz add hozzá pontosan: ",
        "2. Add this Redirect URI exactly: ");
    public static string SpotifyInstrStep3 => T(
        "3. Mentsd, majd a Settings → Basic Information oldalról másold ide a Client ID-t.",
        "3. Save, then copy the Client ID from Settings → Basic Information here.");
    public static string SpotifyInstrStep4 => T(
        "4. A SoundTouch-on már be kell legyen kötve a Spotify (lásd a Spotify app eszközlistáját).",
        "4. Spotify must already be enabled on the SoundTouch (see the device list in the Spotify app).");
    public static string SpotifyClientIdLabel => T("Client ID:", "Client ID:");
    public static string SpotifyConnectAction => T("Csatlakozás Spotify-hoz", "Connect to Spotify");
    public static string SpotifyReconnectAction => T("Újracsatlakozás", "Reconnect");
    public static string SpotifyDisconnectAction => T("Lecsatlakozás", "Disconnect");
    public static string SpotifyNotConnected => T("Nincs csatlakoztatva", "Not connected");
    public static string SpotifyConnectedAs(string user) => F(
        "Csatlakoztatva — {0}", "Connected — {0}", user);
    public static string SpotifyConnectedNoUser => T("Csatlakoztatva.", "Connected.");
    public static string SpotifyDeviceLabel => T(
        "Spotify Connect eszköz (a SoundTouch):",
        "Spotify Connect device (the SoundTouch):");
    public static string SpotifyTestDeviceButton => T("Tesztelés", "Test");
    public static string SpotifyTestDeviceTooltip => T(
        "Átkapcsolja a Spotify lejátszást a kiválasztott eszközre",
        "Switches Spotify playback to the selected device");
    public static string SpotifyRefreshButton => T("Frissít", "Refresh");
    public static string SpotifyDeviceHint => T(
        "Ha a SoundTouch nincs a listában, indíts el rajta egy Spotify lejátszást a telefonról (hogy „felébredjen”), majd Frissít. Ha nem biztos melyik az, a Tesztelés átkapcsol arra az eszközre.",
        "If the SoundTouch is not in the list, start a Spotify playback on it from your phone (to wake it up), then click Refresh. If you're unsure which one it is, click Test to switch to that device.");
    public static string SpotifyDeviceTypeLabel => T("típus:", "type:");
    public static string SpotifyDeviceIdLabel => T("id:", "id:");
    public static string SpotifyDeviceActive => T("● aktív", "● active");

    public static string SpotifyGiveClientId => T("Add meg a Client ID-t.", "Enter the Client ID.");
    public static string SpotifyWaitingLogin => T(
        "Várom a Spotify bejelentkezést a böngészőből…",
        "Waiting for Spotify login from the browser…");
    public static string SpotifyConnectedOk => T(
        "✓ Sikeresen csatlakoztatva.", "✓ Successfully connected.");
    public static string SpotifyDeviceLoadErr(string err) => F(
        "Eszközök lekérdezése sikertelen: {0}",
        "Failed to load devices: {0}", err);
    public static string SpotifyDevicesLoading => T(
        "Eszközök betöltése…", "Loading devices…");
    public static string SpotifyDevicesNone => T(
        "Nem található Spotify Connect eszköz.",
        "No Spotify Connect devices found.");
    public static string SpotifyDevicesCount(int n) => F(
        "{0} eszköz.", "{0} device(s).", n);
    public static string SpotifyDisconnected => T(
        "Lecsatlakoztatva.", "Disconnected.");
    public static string SpotifyConnectFirst => T(
        "Először csatlakozz Spotify-hoz.", "Connect to Spotify first.");
    public static string SpotifySelectDeviceFirst => T(
        "Válassz egy eszközt a tesztelés előtt.",
        "Select a device before testing.");
    public static string SpotifySwitchingTo(string name) => F(
        "Átkapcsolás erre: {0}…", "Switching to: {0}…", name);
    public static string SpotifyTransferredTo(string name) => F(
        "✓ Átkapcsolva: {0} — nézd meg melyik eszköz reagált.",
        "✓ Switched to: {0} — check which device responded.", name);
    public static string SpotifyDeviceSelected(string name) => F(
        "Eszköz kiválasztva: {0}", "Device selected: {0}", name);

    // ---------- Service errors ----------
    public static string UpnpDescUnavailable(string url, string err) => F(
        "UPnP device description nem érhető el: {0} ({1})",
        "UPnP device description unreachable: {0} ({1})", url, err);
    public static string UpnpAvTransportNotFound => T(
        "AVTransport service nem található a leírásban.",
        "AVTransport service not found in description.");
    public static string SoundtouchInfoUnreachable(string url, string err) => F(
        "Soundtouch /info nem érhető el ({0}): {1}",
        "Soundtouch /info unreachable ({0}): {1}", url, err);
    public static string SoundtouchInfoNoDeviceId => T(
        "Az /info válaszban nincs deviceID.",
        "/info response has no deviceID.");
    public static string UpnpControlUrlMissing => T(
        "AVTransport control URL nem található.",
        "AVTransport control URL not found.");
    public static string UpnpActionError(string action, int code, string body) => F(
        "UPnP {0} hiba {1}: {2}", "UPnP {0} error {1}: {2}", action, code, body);
    public static string UpnpClientNotInit => T(
        "UPnP kliens nincs inicializálva.",
        "UPnP client not initialized.");
    public static string SpotifyNotConnectedErr => T(
        "Spotify nincs csatlakoztatva.", "Spotify is not connected.");
    public static string SpotifyDevicePresetMissing => T(
        "Spotify URI van a presetben, de nincs Spotify eszköz kiválasztva. (tray → Spotify Connect…)",
        "Spotify URI in preset, but no Spotify device selected. (tray → Spotify Connect…)");
    public static string SpotifyRedirectPortError(string err) => F(
        "Nem tudtam megnyitni a 127.0.0.1:38765 portot a Spotify visszahíváshoz: {0}",
        "Could not open port 127.0.0.1:38765 for Spotify callback: {0}", err);
    public static string SpotifyBrowserError(string err) => F(
        "Böngésző megnyitás hiba: {0}", "Browser open error: {0}", err);
    public static string SpotifyLoginTimeout => T(
        "Időtúllépés a Spotify-bejelentkezés várásakor.",
        "Spotify login timed out.");
    public static string SpotifyAuthError(string err) => F(
        "Spotify engedélyezési hiba: {0}", "Spotify authorization error: {0}", err);
    public static string SpotifyNoAuthCode => T(
        "Nem érkezett authorization code.", "No authorization code received.");
    public static string SpotifyTokenExchangeError(int code, string body) => F(
        "Token csere hiba {0}: {1}", "Token exchange error {0}: {1}", code, body);
    public static string SpotifyTokenIncomplete => T(
        "Hiányos token válasz.", "Incomplete token response.");
    public static string SpotifyRefreshError(int code, string body) => F(
        "Spotify token frissítés hiba {0}: {1}",
        "Spotify token refresh error {0}: {1}", code, body);
    public static string SpotifyPlayError(int code, string body) => F(
        "Spotify play hiba {0}: {1}", "Spotify play error {0}: {1}", code, body);
    public static string SpotifyTransferError(int code, string body) => F(
        "Spotify transfer hiba {0}: {1}", "Spotify transfer error {0}: {1}", code, body);
    public static string RadioBrowserNoServer => T(
        "A radio-browser szolgáltatás egyik szervere sem elérhető.",
        "No radio-browser server is reachable.");

    // ---------- Spotify auth callback HTML ----------
    public static string SpotifyCallbackOk => T(
        "Sikeres bejelentkezés", "Login successful");
    public static string SpotifyCallbackOkSub => T(
        "Visszatérhetsz az alkalmazáshoz.", "You can return to the application.");
    public static string SpotifyCallbackErr => T("Hiba", "Error");
}
