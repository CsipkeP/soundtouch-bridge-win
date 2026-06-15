using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Navigation;
using BoseSoundTouchBridge.Localization;

namespace BoseSoundTouchBridge;

internal static class HelpContent
{
    private static readonly Brush HeadingFg = new SolidColorBrush(Color.FromRgb(0, 120, 212));
    private static readonly Brush MutedFg = new SolidColorBrush(Color.FromRgb(102, 102, 102));
    private static readonly Brush GreenFg = new SolidColorBrush(Color.FromRgb(46, 160, 67));
    private static readonly Brush AmberFg = new SolidColorBrush(Color.FromRgb(195, 105, 0));
    private static readonly Brush CodeBg = new SolidColorBrush(Color.FromRgb(240, 240, 240));
    private static readonly FontFamily MonoFont = new("Consolas");

    public static FlowDocument Build(RequestNavigateEventHandler onLink)
    {
        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 13,
            PagePadding = new Thickness(24, 16, 24, 16),
            ColumnWidth = 9999,
            LineHeight = 20,
            TextAlignment = TextAlignment.Left
        };

        if (L.Lang == "hu") BuildHu(doc, onLink);
        else BuildEn(doc, onLink);

        return doc;
    }

    // ---------------- helpers ----------------

    private static Paragraph H1(string text) => new(new Run(text))
    {
        FontSize = 22, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 4)
    };

    private static Paragraph Subtitle(string text) => new(new Run(text))
    {
        FontSize = 14, Foreground = MutedFg, Margin = new Thickness(0, 0, 0, 16)
    };

    private static Paragraph H2(string text) => new(new Run(text))
    {
        FontSize = 16, FontWeight = FontWeights.SemiBold, Foreground = HeadingFg,
        Margin = new Thickness(0, 16, 0, 4)
    };

    private static Paragraph H3(string text) => new(new Run(text))
    {
        FontSize = 14, FontWeight = FontWeights.SemiBold,
        Margin = new Thickness(0, 10, 0, 2)
    };

    private static Paragraph P(params Inline[] inlines)
    {
        var p = new Paragraph();
        foreach (var i in inlines) p.Inlines.Add(i);
        return p;
    }

    private static Run R(string text) => new(text);
    private static Bold B(string text) => new(new Run(text));
    private static Bold B(string text, Brush fg) => new(new Run(text)) { Foreground = fg };

    private static Run Mono(string text) => new(text)
    {
        FontFamily = MonoFont, Background = CodeBg
    };

    private static Hyperlink Link(string text, string url, RequestNavigateEventHandler onClick)
    {
        var hl = new Hyperlink(new Run(text)) { NavigateUri = new Uri(url) };
        hl.RequestNavigate += onClick;
        return hl;
    }

    private static List Bullets(params Paragraph[] items)
    {
        var list = new List { MarkerStyle = TextMarkerStyle.Disc, Margin = new Thickness(20, 4, 0, 8) };
        foreach (var b in items) list.ListItems.Add(new ListItem(b));
        return list;
    }

    private static List Numbered(params Paragraph[] items)
    {
        var list = new List { MarkerStyle = TextMarkerStyle.Decimal, Margin = new Thickness(20, 4, 0, 8) };
        foreach (var b in items) list.ListItems.Add(new ListItem(b));
        return list;
    }

    // =================== HUNGARIAN ===================
    private static void BuildHu(FlowDocument doc, RequestNavigateEventHandler onLink)
    {
        doc.Blocks.Add(H1("Bose SoundTouch Bridge"));
        doc.Blocks.Add(Subtitle("Használati útmutató"));

        doc.Blocks.Add(H2("Mire való?"));
        doc.Blocks.Add(P(R(
            "A Bose 2026-os cloud kivezetése után a SoundTouch eszközök fizikai preset gombjai nem működnek úgy mint korábban. " +
            "Ez az alkalmazás figyeli a SoundTouch websocketjét, és amikor megnyomod a 1–6 gombok valamelyikét, lejátssza az ahhoz " +
            "rendelt internetrádiót (UPnP-n keresztül) vagy Spotify lejátszási listát (Spotify Connect API). Az alkalmazás a háttérben " +
            "fut, tálca ikonnal.")));

        doc.Blocks.Add(H2("Első indítás"));
        doc.Blocks.Add(P(R("Indítás után megjelenik a tálca ikon. "), B("Bal klikk"), R(" nyitja a beállítások ablakot. "),
            B("Jobb klikk"), R(" ad egy menüt (Beállítások, Spotify Connect, Súgó, Újrakapcsolódás, Kilépés). " +
            "Az ablak X gombja csak elrejti az alkalmazást, a háttérben tovább fut.")));

        doc.Blocks.Add(H2("SoundTouch IP címe"));
        doc.Blocks.Add(P(R("Ha tudod az IP-t, írd be a felső mezőbe. Ha nem, kattints a "), Mono(" 🔍 "),
            R(" gombra — SSDP-vel megkeresi a hálózati eszközöket. Add hozzá a kívánt SoundTouchot a megjelenő listából, majd "),
            B("Mentés és újrakapcsolódás"), R(".")));
        doc.Blocks.Add(P(
            R("A státusz LED jelentése: szürke = leállítva / nincs IP, sárga = csatlakozás, "),
            B("zöld = csatlakozva", GreenFg), R(", piros = hiba. Mellette az "), Mono(" ⟲ "),
            R(" gomb manuálisan újraindítja a websocket kapcsolatot.")));
        doc.Blocks.Add(P(B("Tipp:"), R(" állíts be fix IP-t a routereden a SoundTouch MAC címéhez, hogy ne változzon DHCP-vel.")));

        doc.Blocks.Add(H2("Eszköz vezérlése"));
        doc.Blocks.Add(P(R("A státusz alatti panelon közvetlenül vezérelheted a SoundTouchot, függetlenül a presetektől:")));
        doc.Blocks.Add(Bullets(
            P(Mono(" ⏻ "), R(" "), B("Be / Ki"), R(" — power toggle (a piros/zöld szín jelzi az állapotot)")),
            P(Mono(" 🔊 / 🔇 "), R(" "), B("Némítás"), R(" — toggle, az ikon mutatja a tényleges állapotot")),
            P(B("Hangerő csúszka"), R(" + +/− gombok (0–100)")),
            P(Mono(" ↻ "), R(" "), B("Frissít"), R(" — aktuális állapot lekérdezése az eszközről"))
        ));

        doc.Blocks.Add(H2("Internetrádió presetek"));
        doc.Blocks.Add(P(R("6 sor, mindegyik megfelel egy fizikai gombnak a SoundTouchon. Kék kerek számos gomb a sor elején — a " +
            "teszteléshez, ugyanazt játssza le mint a fizikai gombnyomás. A sor végén a "), Mono(" 📻 "), R(" gomb megnyitja az " +
            "internetrádió keresőt.")));

        doc.Blocks.Add(H3("Rádió kereső használata"));
        doc.Blocks.Add(P(R("Az adatok a "), Link("radio-browser.info", "https://www.radio-browser.info", onLink),
            R(" közösségi adatbázisból jönnek. Szűrhetsz:")));
        doc.Blocks.Add(Bullets(
            P(B("Ország"), R(" — alapból a Windows régiódra állítva")),
            P(B("Stílus"), R(" — pop, classical, rock, jazz, stb. (top 120 tag)")),
            P(B("Név"), R(" — szabadszöveges keresés"))
        ));
        doc.Blocks.Add(P(R("Az oszlopfejlécre kattintva rendezhetsz. Dupla kattintás vagy "), B("Kiválasztás"),
            R(" beilleszti az állomást a preset URL és Név mezőjébe. Utána "), B("Mentés és újrakapcsolódás"), R(".")));
        doc.Blocks.Add(P(R("Manuális URL beillesztés is működik. A SoundTouch csak HTTP streamet játszik le; ha HTTPS URL-t adsz meg, " +
            "a bridge automatikusan HTTP-re írja.")));

        doc.Blocks.Add(H2("Spotify Connect (lejátszási listák)"));
        doc.Blocks.Add(P(B("Spotify Premium fiók szükséges.", AmberFg), R(" A SoundTouch firmware-ja tartalmazza a Spotify Connect " +
            "támogatást — ez a Spotify saját szerverein fut, a Bose cloud kivezetés nem érintette.")));

        doc.Blocks.Add(H3("Egyszeri beállítás"));
        doc.Blocks.Add(Numbered(
            P(R("Nyisd meg: "),
              Link("developer.spotify.com/dashboard", "https://developer.spotify.com/dashboard", onLink),
              R(" és jelentkezz be a saját Spotify fiókoddal.")),
            P(R("Hozz létre egy új "), B("app"), R("-ot (név pl. \"SoundTouch Bridge\"; a többi mezőt nem kötelező kitölteni).")),
            P(R("A "), B("Redirect URI"), R(" mezőhöz add hozzá pontosan: "), Mono("http://127.0.0.1:38765/callback")),
            P(R("Mentés után a Settings → Basic Information oldalról másold a "), B("Client ID"), R("-t.")),
            P(R("Az alkalmazásban kattints a zöld "), B("Spotify Connect…"), R(" gombra, paszta a Client ID-t, majd "),
              B("Csatlakozás Spotify-hoz"), R(".")),
            P(R("Böngészőben jelentkezz be a Spotify fiókoddal és engedélyezd a hozzáférést. Visszatérés az appba — automatikusan elmenti " +
              "a refresh tokent.")),
            P(B("Aktiváld a SoundTouchot Spotify Connect eszközként"), R(": a telefonod Spotify appjából indíts el bármit, majd a " +
              "lejátszó alján válaszd ki a SoundTouchot az eszközlistából. (Ezt csak az első használat előtt kell.)")),
            P(R("Az appban "), B("Frissít"), R(" gomb — a SoundTouch megjelenik a listában (típus általában \"Speaker\"). Ha nem biztos " +
              "melyik az, a "), B("Tesztelés"), R(" gomb átkapcsolja a Spotify lejátszást a kiválasztott eszközre — láthatóan reagál a " +
              "SoundTouch. Válaszd ki és bezárás."))
        ));

        doc.Blocks.Add(H3("Spotify preset"));
        doc.Blocks.Add(P(R("Egy preset URL mezőjébe paszta egy Spotify linket vagy URI-t:")));
        doc.Blocks.Add(Bullets(
            P(Mono("spotify:playlist:37i9dQZF1DXcBWIGoYBM5M")),
            P(Mono("https://open.spotify.com/playlist/37i9...")),
            P(R("Track, album, artist, podcast (show / episode) is működik"))
        ));
        doc.Blocks.Add(P(R("Spotify linket a Spotify app vagy weben kapsz: jobb klikk a listán → Megosztás → Link másolása. A bridge " +
            "automatikusan felismeri és Spotify Connect Transfer + Play hívással indítja a SoundTouch-on. Internetrádió és Spotify " +
            "presetek keverten is használhatók — preset 1 lehet Bartók Rádió, preset 2 egy Spotify playlist, stb.")));

        doc.Blocks.Add(H2("Gyakori problémák"));
        doc.Blocks.Add(Bullets(
            P(B("Státusz LED nem zöld"), R(" — ellenőrizd az IP-t és a hálózati kapcsolatot. A 8080 TCP portnak elérhetőnek kell lennie " +
              "a SoundTouchon. Tűzfalat is ellenőrizd.")),
            P(B("Internetrádió nem szól"), R(" — a SoundTouch nem támogatja a HTTPS streamet. Az app automatikusan átírja HTTP-re, de " +
              "néhány állomás csak HTTPS-en érhető el, azok nem fognak menni. Próbálj másikat a rádió keresőben.")),
            P(B("Spotify \"No active device\" / 404"), R(" — az eszköz alszik. A telefonról indíts el egy lejátszást a SoundTouch-on " +
              "(Spotify app → eszközválasztó → SoundTouch). A bridge ezután automatikusan újra próbálkozik.")),
            P(B("Spotify \"Premium required\""), R(" — a Connect API csak Premium fiókkal működik. Ingyenes fiókkal a Spotify funkciók " +
              "nem elérhetők.")),
            P(B("Eszközkereső nem talál semmit"), R(" — a SSDP UDP multicast portját (1900) blokkolhatja a tűzfal vagy a router. Add meg " +
              "az IP-t kézzel."))
        ));

        doc.Blocks.Add(H2("Beállítások és adatok"));
        doc.Blocks.Add(P(R("Minden beállítás itt: "), Mono("%APPDATA%\\BoseSoundTouchBridge\\settings.json")));
        doc.Blocks.Add(P(R("Ez tartalmazza az IP-t, presetek nevét és URL-jét, valamint a Spotify Client ID-t és refresh tokent (utóbbi " +
            "titkos, ne oszd meg). Ha új gépre költözöl, ezt a fájlt másolva minden átvitelre kerül.")));

        doc.Blocks.Add(H2("Köszönet"));
        doc.Blocks.Add(P(R("Alapötlet és protokoll-leírás: "),
            Link("sandervg/homeassistant-bose-soundtouch-bridge",
                "https://github.com/sandervg/homeassistant-bose-soundtouch-bridge", onLink),
            R(" (Home Assistant add-on).")));
        doc.Blocks.Add(P(R("Internetrádió adatok: "),
            Link("radio-browser.info", "https://www.radio-browser.info", onLink), R(" közösségi adatbázis.")));
    }

    // =================== ENGLISH ===================
    private static void BuildEn(FlowDocument doc, RequestNavigateEventHandler onLink)
    {
        doc.Blocks.Add(H1("Bose SoundTouch Bridge"));
        doc.Blocks.Add(Subtitle("User guide"));

        doc.Blocks.Add(H2("What is this?"));
        doc.Blocks.Add(P(R(
            "After Bose's 2026 cloud retirement, the physical 1–6 preset buttons on SoundTouch devices stopped working as before. " +
            "This app listens to the SoundTouch websocket, and when you press one of the buttons, plays the configured internet " +
            "radio (via UPnP) or Spotify playlist (Spotify Connect API). The app runs in the background with a tray icon.")));

        doc.Blocks.Add(H2("First start"));
        doc.Blocks.Add(P(R("A tray icon appears after launch. "), B("Left-click"), R(" opens the settings window. "),
            B("Right-click"), R(" shows a menu (Settings, Spotify Connect, Help, Reconnect, Exit). " +
            "The window's X button only hides the app — it keeps running in the background.")));

        doc.Blocks.Add(H2("SoundTouch IP address"));
        doc.Blocks.Add(P(R("If you know the IP, type it in the top field. If not, click the "), Mono(" 🔍 "),
            R(" button — SSDP discovery scans the network. Pick your SoundTouch from the list, then "),
            B("Save and reconnect"), R(".")));
        doc.Blocks.Add(P(
            R("Status LED meaning: gray = stopped / no IP, yellow = connecting, "),
            B("green = connected", GreenFg), R(", red = error. The "), Mono(" ⟲ "),
            R(" button next to it manually reconnects the websocket.")));
        doc.Blocks.Add(P(B("Tip:"), R(" reserve a fixed IP for the SoundTouch's MAC in your router so it doesn't change with DHCP.")));

        doc.Blocks.Add(H2("Device control"));
        doc.Blocks.Add(P(R("The panel below the status lets you control the SoundTouch directly, independent of presets:")));
        doc.Blocks.Add(Bullets(
            P(Mono(" ⏻ "), R(" "), B("Power"), R(" — toggle (red/green color indicates state)")),
            P(Mono(" 🔊 / 🔇 "), R(" "), B("Mute"), R(" — toggle, icon reflects current state")),
            P(B("Volume slider"), R(" + +/− buttons (0–100)")),
            P(Mono(" ↻ "), R(" "), B("Refresh"), R(" — query current state from the device"))
        ));

        doc.Blocks.Add(H2("Internet radio presets"));
        doc.Blocks.Add(P(R("6 rows, each matching one physical button on the SoundTouch. The blue numbered button on the left of each " +
            "row tests the preset (same as a physical button press). At the right end of the row, the "), Mono(" 📻 "),
            R(" button opens the radio browser.")));

        doc.Blocks.Add(H3("Using the radio browser"));
        doc.Blocks.Add(P(R("Data comes from the "), Link("radio-browser.info", "https://www.radio-browser.info", onLink),
            R(" community database. You can filter by:")));
        doc.Blocks.Add(Bullets(
            P(B("Country"), R(" — defaults to your Windows region")),
            P(B("Genre"), R(" — pop, classical, rock, jazz, etc. (top 120 tags)")),
            P(B("Name"), R(" — free-text search"))
        ));
        doc.Blocks.Add(P(R("Click a column header to sort. Double-click or "), B("Select"),
            R(" inserts the station into the preset's URL and Name fields. Then "), B("Save and reconnect"), R(".")));
        doc.Blocks.Add(P(R("Pasting a URL manually also works. The SoundTouch only plays HTTP streams; HTTPS URLs are " +
            "automatically rewritten to HTTP by the bridge.")));

        doc.Blocks.Add(H2("Spotify Connect (playlists)"));
        doc.Blocks.Add(P(B("Spotify Premium account required.", AmberFg),
            R(" The SoundTouch firmware includes Spotify Connect support — this runs on Spotify's own servers and was not affected " +
              "by the Bose cloud retirement.")));

        doc.Blocks.Add(H3("One-time setup"));
        doc.Blocks.Add(Numbered(
            P(R("Open: "),
              Link("developer.spotify.com/dashboard", "https://developer.spotify.com/dashboard", onLink),
              R(" and log in with your Spotify account.")),
            P(R("Create a new "), B("app"), R(" (e.g. name: \"SoundTouch Bridge\"; other fields optional).")),
            P(R("To the "), B("Redirect URI"), R(" field add exactly: "), Mono("http://127.0.0.1:38765/callback")),
            P(R("After saving, copy the "), B("Client ID"), R(" from Settings → Basic Information.")),
            P(R("In the app click the green "), B("Spotify Connect…"), R(" button, paste the Client ID, then "),
              B("Connect to Spotify"), R(".")),
            P(R("Sign in to your Spotify account in the browser and grant access. Returning to the app — refresh token is saved automatically.")),
            P(B("Activate the SoundTouch as a Spotify Connect device"), R(": from your phone's Spotify app, start any playback, then in " +
              "the player pick the SoundTouch from the devices list. (Only needed before first use.)")),
            P(R("In the app click "), B("Refresh"), R(" — the SoundTouch shows up in the list (type is usually \"Speaker\"). If you're " +
              "unsure which one it is, the "), B("Test"), R(" button switches Spotify playback to the selected device — the SoundTouch " +
              "will visibly respond. Pick it and close."))
        ));

        doc.Blocks.Add(H3("Spotify preset"));
        doc.Blocks.Add(P(R("Paste a Spotify link or URI into a preset's URL field:")));
        doc.Blocks.Add(Bullets(
            P(Mono("spotify:playlist:37i9dQZF1DXcBWIGoYBM5M")),
            P(Mono("https://open.spotify.com/playlist/37i9...")),
            P(R("Track, album, artist, podcast (show / episode) also work"))
        ));
        doc.Blocks.Add(P(R("Get a Spotify link from the Spotify app or web: right-click an item → Share → Copy link. The bridge " +
            "automatically recognises it and starts playback via Spotify Connect Transfer + Play on the SoundTouch. Internet radio and " +
            "Spotify presets can be mixed — preset 1 can be BBC Radio, preset 2 a Spotify playlist, etc.")));

        doc.Blocks.Add(H2("Common issues"));
        doc.Blocks.Add(Bullets(
            P(B("Status LED not green"), R(" — check the IP and network. TCP port 8080 must be reachable on the SoundTouch. Check the firewall too.")),
            P(B("Internet radio not playing"), R(" — the SoundTouch doesn't support HTTPS streams. The app rewrites HTTPS to HTTP " +
              "automatically, but some stations only serve HTTPS — those won't work. Try another in the radio browser.")),
            P(B("Spotify \"No active device\" / 404"), R(" — the device is asleep. From your phone, start a playback on the SoundTouch " +
              "(Spotify app → device picker → SoundTouch). The bridge then retries automatically.")),
            P(B("Spotify \"Premium required\""), R(" — the Connect API only works with Premium. Free accounts can't use Spotify features.")),
            P(B("Device discovery finds nothing"), R(" — the SSDP UDP multicast port (1900) may be blocked by your firewall or router. " +
              "Enter the IP manually."))
        ));

        doc.Blocks.Add(H2("Settings and data"));
        doc.Blocks.Add(P(R("All settings here: "), Mono("%APPDATA%\\BoseSoundTouchBridge\\settings.json")));
        doc.Blocks.Add(P(R("It contains the IP, preset names and URLs, plus the Spotify Client ID and refresh token (the latter is " +
            "secret — don't share). When moving to a new machine, copying this file transfers everything.")));

        doc.Blocks.Add(H2("Credits"));
        doc.Blocks.Add(P(R("Original idea and protocol description: "),
            Link("sandervg/homeassistant-bose-soundtouch-bridge",
                "https://github.com/sandervg/homeassistant-bose-soundtouch-bridge", onLink),
            R(" (Home Assistant add-on).")));
        doc.Blocks.Add(P(R("Internet radio data: "),
            Link("radio-browser.info", "https://www.radio-browser.info", onLink), R(" community database.")));
    }
}
