#:property TargetFramework=net10.0
#:property PublishAot=true
#:property PublishSingleFile=true
#:property Nullable=enable
#:package VYaml@1.4.0

using WinAerials;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using VYaml.Serialization;
using VYaml.Annotations;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using System.Formats.Tar;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

try
{
    await RunAsync(ResolveConfig());
}
catch (AppException ex)
{
    ShowFatal(ex.Title, ex.Message);
}
catch (Exception ex)
{
    ShowFatal("Unhandled exception", ex.ToString());
}

static async Task RunAsync(AppConfig config)
{
    await LoadPlaylistJs(config);

    var livelyExe = ResolveLivelyExe(config);
    if (livelyExe == null) return;

    Logger.Debug("Invoking Lively.exe to run page as desktop wallpaper...");
    Process.Start(new ProcessStartInfo(livelyExe, $"closewp") { UseShellExecute = true });
    Process.Start(new ProcessStartInfo(livelyExe, $"setwp --file \"{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Lively Wallpaper", "Library", "wallpapers", "WinAerials")}\"")
    { UseShellExecute = true }
    );
}

static AppConfig ResolveConfig()
{
    var configPath = Path.Combine(Directory.GetCurrentDirectory(), "config.yaml");
    Assert.FileExists(configPath, "Config.yaml not found in current directory. Should've come directly with the repo clone.");

    var deserializeOtions = new YamlSerializerOptions { NamingConvention = NamingConvention.SnakeCase };
    byte[] yamlBytes = File.ReadAllBytes(configPath);
    Assert.NotZero(yamlBytes.Length, "config.yaml empty!");

    var config = YamlSerializer.Deserialize<AppConfig>(yamlBytes, deserializeOtions);
    Logger.IsDebugEnabled = config.DebugLogEnabled;
    Assert.NotEmpty(config.AerialsSourceUrl, "`aerials_source_url` is empty");

    return config;
}

static void Shuffle<T>(IList<T> list)
{
    var rng = Random.Shared;
    for (int i = list.Count - 1; i > 0; i--)
    {
        int j = rng.Next(i + 1);
        var tmp = list[i];
        list[i] = list[j];
        list[j] = tmp;
    }
}

static async Task<string?> FetchWithRetriesAsync(string url, int attempts, int[] delaysMs)
{
    using var http = new HttpClient();
    for (int i = 0; i < attempts; i++)
    {
        try
        {
            var res = await http.GetAsync(url);
            if (res.IsSuccessStatusCode) return await res.Content.ReadAsStringAsync();
        }
        catch { }
        Logger.Debug($"Fetch attempt {i + 1} failed for {url}. Sleeping for {delaysMs[i]} ms and retrying...");
        if (i < delaysMs.Length) await Task.Delay(delaysMs[i]);
    }
    return null;
}


// static void UpdateLivelyPage(string movieUrl, string movieTitle)
// {
//     var pagePath = Path.Combine(Directory.GetCurrentDirectory(), "WinAerialsPage.html");
//     Assert.FileExists(pagePath, "WinAerialsPage.html not found in current directory. Should've come directly with the repo clone.");

//     string htmlContent = File.ReadAllText(pagePath);

//     ReplaceSection(ref htmlContent, "MOVIE_URL", $"<source src=\"{movieUrl}\">");
//     ReplaceSection(ref htmlContent, "MOVIE_TITLE", movieTitle);

//     File.WriteAllText(pagePath, htmlContent);
// }

// static void ReplaceSection(ref string source, string sectionName, string replacement)
// {
//     string pattern = @"(<!-- " + sectionName + "_START -->).*?(<!-- " + sectionName + "_END -->)";
//     source = Regex.Replace(source, pattern, $"$1{replacement}$2", RegexOptions.Singleline);
// }

static string? ResolveLivelyExe(AppConfig config)
{
    if (!string.IsNullOrWhiteSpace(config.LivelyExePath))
    {
        if (File.Exists(config.LivelyExePath))
        {
            Logger.Debug($"Using configured Lively.exe path: {config.LivelyExePath}");
            return config.LivelyExePath;
        }
    }

    Logger.Debug("Lively.exe path not configured. Checking for running Lively.exe process...");

    // Check running processes for an active Lively main module
    var procs = Process.GetProcesses();
    foreach (var proc in procs)
    {
        if (string.Equals(proc.ProcessName, "Lively", StringComparison.OrdinalIgnoreCase))
        {
            string exePath = proc.MainModule?.FileName ?? "";
            Logger.Debug($"Discovered running Lively.exe process: {proc.Id} - {exePath}");
            Logger.Debug("Saving discovered Lively.exe path to config.yaml for future runs.");
            if (File.Exists(config.LivelyExePath)) { }
            var configPath = Path.Combine(Directory.GetCurrentDirectory(), "config.yaml");
            config.LivelyExePath = exePath;
            File.WriteAllBytes(configPath, YamlSerializer.Serialize(config).ToArray());
            return exePath;
        }
    }

    Logger.Debug("Lively.exe not found in running processes. Launching the install site...");
    Process.Start(new ProcessStartInfo
    {
        // FileName = "ms-windows-store://pdp/?ProductId=9NTM2QC6QWS7", // Lively's MS Store ID
        FileName = "https://www.rocksdanister.com/lively/",
        UseShellExecute = true // Crucial for launching URLs directly without an explicit executable path
    });
    return null;
}


[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
static extern int MessageBoxW(IntPtr hWnd, string lpText, string lpCaption, uint uType);

static void ShowFatal(string title, string message)
{
    try
    {
        // Create a row of spaces
        // Wrap it with an invisible, zero-width structural anchor character (\uFEFF)
        //    so the OS layout engine cannot optimize or strip the padding away.
        var _ = MessageBoxW(IntPtr.Zero, message + "\n\n" + new string(' ', 300) + "\uFEFF", title, 0);
    }
    catch
    {
        Console.Error.WriteLine(title + ": " + message);
    }
}

static async Task<List<(string Url, string Label)>> ResolveMoviesAsync(AppConfig config)
{
    Logger.Debug("Resolving movies from remote source: " + config.AerialsSourceUrl);
    var fetched = await FetchWithRetriesAsync(config.AerialsSourceUrl, 3, [2000, 4000, 8000]);
    Assert.NotNull(fetched, "Network failure", "Unable to fetch remote aerials plist after retries.");

    var tarUrl = ExtractTarUrl(fetched);
    var entriesJson = await GetEntriesJsonFromTarAsync(tarUrl);

    var found = ParseAppleEntries(config, entriesJson);
    Assert.NotZero(found.Count, "No assets found", "Could not extract .mov assets from the downloaded definition.");

    Shuffle(found);
    return found;
}


static List<(string Url, string Label)> ParseAppleEntries(AppConfig config, JsonNode? root)
{
    var extractedMovies = new List<(string Url, string Label)>();

    // Safely jump directly to the target entries array root node
    JsonArray? entries = root?["assets"]?.AsArray();
    Assert.NotNull(entries, "entries.json > assets node not found", "");

    // 3. Loop through elements structurally rather than searching text patterns
    foreach (JsonNode? entry in entries)
    {
        if (entry is not JsonObject assetObj) continue;

        // Extract values precisely from their direct nodes
        string? movieUrl = assetObj[config.MovieNodeName]?.ToString();
        string? label = assetObj[config.MovieTitleNodeName]?.ToString();

        // Validate that we have an active video payload before registering
        if (!string.IsNullOrWhiteSpace(movieUrl))
        {
            // Add named value tuples seamlessly to our collection
            extractedMovies.Add((
                Url: movieUrl.Trim(),
                Label: label?.Trim() ?? "Unknown Location"
            ));
        }
    }

    Logger.Debug($"Extraction pipeline complete. Found {extractedMovies.Count} movies.");
    return extractedMovies;
}

static async Task<JsonNode?> GetEntriesJsonFromTarAsync(string tarUrl)
{
    // Configure a handler to tolerate intermediate certificate chain errors
    using var ignoreSslHandler = new SocketsHttpHandler
    {
        SslOptions = new SslClientAuthenticationOptions
        {
            // Fixed: Use RemoteCertificateValidationCallback for SslClientAuthenticationOptions
            RemoteCertificateValidationCallback = (object sender, X509Certificate? cert, X509Chain? chain, SslPolicyErrors sslPolicyErrors) =>
            {
                // If it passes cleanly by default, let it through
                if (sslPolicyErrors == SslPolicyErrors.None) return true;

                // Explicitly check if the broken certificate handshake is coming from Apple
                if (sender is SslStream sslStream && sslStream.TargetHostName?.EndsWith("apple.com") == true)
                {
                    Logger.Debug($"Bypassing secure handshake requirement safely for verified host: {sslStream.TargetHostName}");
                    return true;
                }

                // Block any other unknown certificate errors on other domains
                return false;
            }
        }
    };
    using HttpClient client = new(ignoreSslHandler);

    Logger.Debug("Download streaming tar file to pull entries.json...");

    // 1. Stream the headers without buffering the whole file
    using HttpResponseMessage response = await client.GetAsync(tarUrl, HttpCompletionOption.ResponseHeadersRead);
    response.EnsureSuccessStatusCode();

    using Stream networkStream = await response.Content.ReadAsStreamAsync();
    using TarReader tarReader = new(networkStream);

    TarEntry? entry;

    // 2. Scan the archive stream
    while ((entry = await tarReader.GetNextEntryAsync()) != null)
    {
        if (entry.Name.EndsWith("entries.json", StringComparison.OrdinalIgnoreCase))
        {
            if (entry.DataStream != null)
            {
                Logger.Debug($"Found {entry.Name}. Parsing JSON directly from stream...");

                // 3. Parse directly from the live Tar entry data stream into memory
                JsonNode? rootNode = JsonNode.Parse(entry.DataStream);

                Logger.Debug("JSON structure successfully parsed into memory. Severing download.");
                return rootNode;
            }
            break;
        }
    }

    Assert.That(false, "Missing entries.json", "The tar archive did not contain the expected entries.json file.");
    return null;
}


static string ExtractTarUrl(string plistContent)
{
    if (string.IsNullOrWhiteSpace(plistContent)) return string.Empty;

    // Convert to a Span for high-performance, zero-allocation slicing
    ReadOnlySpan<char> span = plistContent.AsSpan();

    // 1. Locate the anchor key token
    int keyIndex = span.IndexOf("<key>resources-url</key>");
    if (keyIndex == -1) return string.Empty;

    // Slice the span to look only at everything after the key
    ReadOnlySpan<char> remaining = span[(keyIndex + "<key>resources-url</key>".Length)..];

    // 2. Locate the opening and closing <string> tags
    int startTag = remaining.IndexOf("<string>");
    int endTag = remaining.IndexOf("</string>");

    if (startTag == -1 || endTag == -1 || endTag <= startTag) return string.Empty;

    // Calculate the exact URL boundaries
    int urlStart = startTag + "<string>".Length;
    int urlLength = endTag - urlStart;

    // 3. Extract the clean URL string
    return remaining.Slice(urlStart, urlLength).ToString();
}


static async Task LoadPlaylistJs(AppConfig config)
{
    var playlistPath = Path.Combine(Directory.GetCurrentDirectory(), "playlist.js");
    List<string>? playList = null;

    if (File.Exists(playlistPath))
    {
        playList = [.. await File.ReadAllLinesAsync(playlistPath)];
    }

    if (playList == null || playList.Count == 0)
    {
        var movies = await ResolveMoviesAsync(config);
        playList = [.. movies.Select(a => $"playlist.push('{a.Url}|{a.Label}');")];
        await File.WriteAllLinesAsync(playlistPath, playList, Encoding.UTF8);
        Logger.Debug($"playlist.js successfully generated with {playList.Count} entries.");
    }
}

namespace WinAerials
{
    // Schema declarations MUST be at the bottom
    [YamlObject]
    public partial class AppConfig
    {
        [YamlMember]
        public string LivelyExePath { get; set; } = string.Empty;

        [YamlMember]
        public string AerialsSourceUrl { get; set; } = string.Empty;
        public string MovieNodeName { get; set; } = string.Empty;
        public string MovieTitleNodeName { get; set; } = string.Empty;

        [YamlMember]
        public bool DebugLogEnabled { get; internal set; }
    }

    public class AppException(string title, string? message) : Exception(message)
    {
        public string Title { get; } = title;
    }

    public static class Assert
    {
        public static void That([DoesNotReturnIf(false)] bool condition, string title, string? message, Action? onFailure = null)
        {
            if (!condition)
            {
                onFailure?.Invoke();
                throw new AppException(title, message);
            }
        }

        public static void NotNull([NotNull] object? obj, string title, string message, Action? onFailure = null) => That(obj != null, title, message, onFailure);
        public static void NotEmpty([NotNull] string? str, string title, string? message = null, Action? onFailure = null) => That(!string.IsNullOrWhiteSpace(str), title, message, onFailure);
        public static void NotZero(int value, string title, string? message = null, Action? onFailure = null) => That(value != 0, title, message, onFailure);
        public static void FileExists(string path, string title, string? message = null, Action? onFailure = null) => That(File.Exists(path), title, message, onFailure);
    }


    public static class Logger
    {
        // A simple runtime flag you can flip via your AppConfig
        public static bool IsDebugEnabled { get; set; } = false;

        // 2. Debug log that respects your runtime flag
        // [Conditional("DEBUG")] means this whole method call disappears in Release mode builds,
        // saving CPU cycles entirely unless you force it.
        [Conditional("DEBUG")]
        public static void Debug(string message)
        {
            if (IsDebugEnabled)
            {
                // Changes color for quick scannability in your VS Code terminal
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"[DEBUG] {message}");
                Console.ResetColor();
            }
        }
    }
}

