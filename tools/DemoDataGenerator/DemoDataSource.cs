namespace Aptabase.Tools.DemoDataGenerator;

public static class DemoDataSource
{
    private static Random _random = new Random();
    private static string[] sdk_version = new[] { "en_US", "en_IE", "de_DE", "pt_PT", "pt_BR", "es_ES" };
    private static string[] locales = new[] { "en_US", "en_IE", "de_DE", "pt_PT", "pt_BR", "es_ES" };
    private static string[] events = new[] { "play_music", "skip_music", "like_music", "add_to_playlist", "remove_from_playlist", "change_volume" };

    private static (string, string)[] musics = new[] {
        ("Adele","Hello"),
        ("Drake","In My Feelings"),
        ("Beyoncé","Formation"),
        ("Taylor Swift","Shake It Off"),
        ("Ed Sheeran","Thinking Out Loud"),
        ("Ariana Grande","7 Rings"),
        ("Post Malone","Sunflower"),
        ("Billie Eilish","Ocean Eyes"),
        ("Bruno Mars","That's What I Like"),
        ("Rihanna","We Found Love"),
        ("Justin Bieber","Sorry"),
        ("Lady Gaga","Bad Romance"),
        ("Shawn Mendes","Stitches"),
        ("Khalid","Talk"),
        ("Dua Lipa","New Rules"),
        ("The Weeknd","Blinding Lights"),
        ("Katy Perry","Firework"),
        ("Maroon 5","Sugar"),
        ("Lizzo","Truth Hurts"),
        ("Sam Smith","Stay with Me"),
        ("Halsey","Without Me"),
        ("Selena Gomez","Lose You to Love Me"),
        ("Coldplay","Viva La Vida"),
        ("Imagine Dragons","Believer"),
        ("Post Malone","Rockstar"),
        ("Beyoncé","Single Ladies (Put a Ring on It)"),
        ("Kendrick Lamar","HUMBLE."),
        ("Bruno Mars","24K Magic"),
        ("Justin Timberlake","SexyBack"),
        ("Billie Eilish","When the Party's Over"),
        ("Ariana Grande","No Tears Left to Cry"),
        ("Dua Lipa","Don't Start Now"),
        ("Sia","Chandelier"),
        ("Ed Sheeran","Photograph"),
        ("Adele","Rolling in the Deep"),
        ("Taylor Swift","Blank Space"),
        ("Calvin Harris","This Is What You Came For"),
        ("The Chainsmokers","Closer"),
        ("Panic! at the Disco","High Hopes"),
        ("Twenty One Pilots","Stressed Out"),
        ("Cardi B","Bodak Yellow"),
        ("Drake","One Dance"),
        ("Lil Nas X","Old Town Road"),
        ("Camila Cabello","Havana"),
        ("BTS","Dynamite"),
        ("Harry Styles","Watermelon Sugar"),
        ("Shawn Mendes, Camila Cabello","Señorita"),
        ("The Weeknd, Ariana Grande","Save Your Tears (Remix)"),
        ("Justin Bieber","Peaches"),
        ("Olivia Rodrigo","Drivers License"),
    };

    private static (string, string, string)[] locations = new[] {
        ("BR", "São Paulo", "São Paulo"),
        ("BR", "Rio de Janeiro", "Rio de Janeiro"),
        ("BR", "Santa Catarina", "Joinville"),
        ("BR", "Santa Catarina", "Florianópolis"),
        ("BR", "Santa Catarina", "Blumenau"),

        ("US", "California", "San Francisco"),
        ("US", "California", "San Jose"),
        ("US", "California", "Oakland"),
        ("US", "New York", "New York City"),
        ("US", "New York", "Brookhaven"),
        ("US", "Texas", "Houston"),
        ("US", "Texas", "San Antonio"),
        ("US", "Texas", "Dallas"),

        ("DE", "Berlin", "Berlin"),
        ("DE", "Bavaria", "Munich"),
        ("DE", "Bavaria", "Nuremberg"),
        ("DE", "Bavaria", "Augsburg"),

        ("IE", "Dublin", "Dublin"),
        ("IE", "Dublin", "Lusk"),
        ("IE", "Dublin", "Rush"),
        ("IE", "Dublin", "Rush"),
        ("IE", "Kildare", "Kildare"),
        ("IE", "Kildare", "Maynooth"),
        ("IE", "Kildare", "Naas"),
        ("IE", "Kildare", "Naas"),
        ("IE", "Kildare", "Naas"),

        ("FR", "Île-de-France", "Paris"),
        ("FR", "Provence-Alpes-Côte d'Azur", "Nice"),
        ("FR", "Occitanie", "Toulouse"),
        ("FR", "Brittany", "Rennes"),
        ("FR", "Auvergne-Rhône-Alpes", "Lyon"),

        ("CA", "Ontario", "Toronto"),
        ("CA", "Quebec", "Montreal"),
        ("CA", "British Columbia", "Vancouver"),
        ("CA", "Alberta", "Calgary"),
        ("CA", "Nova Scotia", "Halifax"),

        ("JP", "Tokyo", "Tokyo"),
        ("JP", "Osaka", "Osaka"),
        ("JP", "Hokkaido", "Sapporo"),
        ("JP", "Kyoto", "Kyoto"),
        ("JP", "Aichi", "Nagoya"),

        ("GB", "England", "London"),
        ("GB", "England", "Manchester"),
        ("GB", "Scotland", "Edinburgh"),
        ("GB", "Wales", "Cardiff"),
        ("GB", "Northern Ireland", "Belfast"),

        ("MX", "Mexico City", "Mexico City"),
        ("MX", "Jalisco", "Guadalajara"),
        ("MX", "Nuevo León", "Monterrey"),
        ("MX", "Baja California", "Tijuana"),
        ("MX", "Quintana Roo", "Cancún"),
    };

    private static (string, string, string, string)[] devices = new[] {
        ("visionOS", "1.0.0", "", ""),
        ("visionOS", "1.0.2", "", ""),
        ("visionOS", "1.0.3", "", ""),
        ("iPadOS", "16.4", "", ""),
        ("iPadOS", "16.3", "", ""),
        ("iPadOS", "17.0", "", ""),
        ("watchOS", "10.0", "", ""),
        ("watchOS", "10.0", "", ""),
        ("watchOS", "9.4", "", ""),
        ("iOS", "16.4", "", ""),
        ("iOS", "16.2", "", ""),
        ("iOS", "17.0", "", ""),
        ("macOS", "13.2.1", "WebKit", "16611.2.7.1.4"),
        ("macOS", "13.1", "WebKit", "17612.4.9.1.8"),
        ("macOS", "13.0.1", "WebKit", "17613.2.7.1.8"),
        ("macOS", "13.0", "WebKit", "17613.2.7.1.8"),
        ("macOS", "12.6.4", "WebKit", "17612.4.9.1.8"),
        ("macOS", "12.6.3", "WebKit", "17612.4.9.1.8"),
        ("macOS", "12.6.2", "WebKit", "17613.2.7.1.8"),
        ("macOS", "12.6.1", "WebKit", "16611.2.7.1.4"),
        ("macOS", "12.4", "WebKit", "16611.2.7.1.4"),
        ("macOS", "12.3", "WebKit", "17613.2.7.1.8"),
        ("Windows", "11.0.22621", "WebView2", "111.0.1661.44"),
        ("Windows", "11.0.22000", "WebView2", "110.0.1587.69"),
        ("Windows", "10.0.19045", "WebView2", "111.0.1661.44"),
        ("Windows", "10.0.18362", "WebView2", "110.0.1587.69"),
        ("Windows", "10.0.17134", "WebView2", "111.0.1661.44"),
        ("Windows", "10.0.15063", "WebView2", "110.0.1587.69"),
        ("Windows", "10.0.10240", "WebView2", "111.0.1661.44"),
        ("Windows", "8.1.9600", "WebView2", "111.0.1661.44"),
        ("Windows", "8.0.9200", "WebView2", "110.0.1587.69"),
        ("Windows", "7.0.7601", "WebView2", "111.0.1661.44"),
        ("Ubuntu", "22.10", "WebKitGTK", "2.38.5"),
        ("Ubuntu", "22.04", "WebKitGTK", "2.38.5"),
        ("Ubuntu", "20.10", "WebKitGTK", "2.38.5"),
        ("Ubuntu", "20.04", "WebKitGTK", "2.32.4"),
        ("Ubuntu", "18.10", "WebKitGTK", "2.32.4"),
        ("Ubuntu", "18.04", "WebKitGTK", "2.32.4"),
        ("Arch Linux", "Unknown", "WebKitGTK", "2.40.0"),
        ("Arch Linux", "RollingRelease", "WebKitGTK", "2.40.0"),
        ("Linux Mint", "21.1", "WebKitGTK", "2.40.0"),
    };

    private static (string, string, string)[] appVersions = new[] {
        ("3.3.2", "", "tauri-plugin-aptabase@1.0.1"),
        ("3.3.1", "", "tauri-plugin-aptabase@1.0.1"),
        ("3.3.0", "", "tauri-plugin-aptabase@1.0.1"),
        ("3.2.0", "", "tauri-plugin-aptabase@1.0.0"),
        ("3.1.1", "", "tauri-plugin-aptabase@1.0.0"),
        ("3.1.0", "", "tauri-plugin-aptabase@1.0.0"),
    };

    public static (string, DateTime[]) NewSession(DateTime startedAt, int maxEventsPerSession)
    {
        var eventsCount = _random.Next(2, maxEventsPerSession);
        var sessionId = Guid.NewGuid().ToString().ToLower();

        var timestamps = Enumerable.Range(0, eventsCount)
            .Select(i => startedAt.AddSeconds(i * 15).AddSeconds(_random.Next(1, 10)))
            .ToArray();

        return (sessionId, timestamps);
    }

    public static int RandomSessionCount() => _random.Next(3, 11);

    public static (string, string, string) GetLocation()
    {
        var index = _random.Next(0, locations.Length);
        return locations[index];
    }

    public static (string, string, string, string) GetDeviceInfo()
    {
        var index = _random.Next(0, devices.Length);
        return devices[index];
    }

    public static (string, string, string) GetAppInfo()
    {
        var index = _random.Next(0, appVersions.Length);
        return appVersions[index];
    }

    public static string GetLocale()
    {
        var index = _random.Next(0, locales.Length);
        return locales[index];
    }

    public static string GetRandomIpAddress()
    {
        return $"10.0.0.{_random.Next(0, 255)}";
    }

    public static (string, Dictionary<string, object>) NewEvent()
    {
        var eventName = events[_random.Next(0, events.Length)];
        var (artist, music) = musics[_random.Next(0, musics.Length)];

        var props = new Dictionary<string, object>();

        if (eventName == "change_volume")
        {
            props.Add("volume", _random.Next(10, 90));
            return (eventName, props);
        }

        props.Add("artist", artist);
        props.Add("music", music);
        if (eventName == "like_music")
            props.Add("like_count", _random.Next(1, 10));

        return (eventName, props);
    }
}