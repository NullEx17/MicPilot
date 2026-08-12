using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text.Json;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DrawingIcon = System.Drawing.Icon;

namespace MicPilot.App.Services;

public static class GameIconResolver
{
    private static readonly ConcurrentDictionary<string, ImageSource> Cache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, string> RememberedPaths = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object PathFileLock = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    static GameIconResolver()
    {
        LoadRememberedPaths();
    }

    public static ImageSource? Resolve(string? processName, string? profileName)
    {
        var cacheKey = $"{processName}|{profileName}";
        if (Cache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        if (TryGetSpec(processName, profileName, out var slug, out _))
        {
            var bundled = LoadPackedPng(slug);
            if (bundled is not null)
            {
                Cache[cacheKey] = bundled;
                return bundled;
            }
        }

        var fromExe = TryFromExecutable(processName);
        if (fromExe is not null)
        {
            Cache[cacheKey] = fromExe;
            return fromExe;
        }

        return null;
    }

    public static void Remember(string processName, string fullPath)
    {
        if (string.IsNullOrWhiteSpace(processName) || string.IsNullOrWhiteSpace(fullPath) || !File.Exists(fullPath))
        {
            return;
        }

        RememberedPaths[StripExe(processName)] = fullPath;
        SaveRememberedPaths();
        Invalidate(processName);
    }

    public static void Invalidate(string? processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
        {
            return;
        }

        var prefix = processName + "|";
        foreach (var key in Cache.Keys)
        {
            if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                key.StartsWith(StripExe(processName) + "|", StringComparison.OrdinalIgnoreCase))
            {
                Cache.TryRemove(key, out _);
            }
        }
    }

    public static bool TryGetSpec(string? processName, string? profileName, out string slug, out string hex)
    {
        slug = "";
        hex = "";

        var process = StripExe(processName);
        var name = (profileName ?? "").Trim().ToLowerInvariant();

        if (Matches(process, "fivem") || name.Contains("fivem") || name.Contains("five m"))
        {
            slug = "fivem";
            hex = "32DC5F";
            return true;
        }

        if (Matches(process, "redm") || name.Contains("redm") || name.Contains("red dead"))
        {
            slug = "redm";
            hex = "E03C31";
            return true;
        }

        if (Matches(process, "valorant") || name.Contains("valorant"))
        {
            slug = "valorant";
            hex = "FF4655";
            return true;
        }

        if (Matches(process, "cs2", "csgo", "cstrike") || name is "cs2" or "csgo" || name.Contains("counter-strike") || name.Contains("counter strike"))
        {
            slug = "counterstrike";
            hex = "F0A030";
            return true;
        }

        if (Matches(process, "gta5", "gta5_enhanced", "playgtav", "gtavlauncher") ||
            name.Contains("gta") || name.Contains("grand theft"))
        {
            slug = "rockstargames";
            hex = "FCAF17";
            return true;
        }

        if (Matches(process, "fortniteclient-win64-shipping", "fortnitelauncher", "fortnite") || name.Contains("fortnite"))
        {
            slug = "fortnite";
            hex = "FFFFFF";
            return true;
        }

        if (Matches(process, "league of legends", "leagueclient", "leagueclientux") || name.Contains("league"))
        {
            slug = "leagueoflegends";
            hex = "C28F2C";
            return true;
        }

        if (Matches(process, "robloxplayerbeta", "robloxplayer", "roblox") || name.Contains("roblox"))
        {
            slug = "roblox";
            hex = "E2231A";
            return true;
        }

        if (Matches(process, "minecraft.windows", "minecraft") || name.Contains("minecraft"))
        {
            slug = "minecraft";
            hex = "62B47A";
            return true;
        }

        if (Matches(process, "rustclient", "rust") || name == "rust")
        {
            slug = "rust";
            hex = "CD412B";
            return true;
        }

        if (Matches(process, "dota2") || name.Contains("dota"))
        {
            slug = "dota2";
            hex = "D32C2C";
            return true;
        }

        if (Matches(process, "tslgame", "pubg") || name.Contains("pubg") || name.Contains("battlegrounds"))
        {
            slug = "pubg";
            hex = "F2A900";
            return true;
        }

        if (Matches(process, "discord", "discordptb", "discordcanary") || name.Contains("discord"))
        {
            slug = "discord";
            hex = "5865F2";
            return true;
        }

        if (Matches(process, "steam") || name == "steam")
        {
            slug = "steam";
            hex = "66C0F4";
            return true;
        }

        if (Matches(process, "epicgameslauncher", "fortniteclient-win64-shipping_eac_eos") || name.Contains("epic"))
        {
            slug = "epicgames";
            hex = "FFFFFF";
            return true;
        }

        if (Matches(process, "battle.net") || name.Contains("battle.net") || name.Contains("battlenet"))
        {
            slug = "battledotnet";
            hex = "00AEFF";
            return true;
        }

        if (Matches(process, "eadesktop", "origin") || name is "ea" or "origin" || name.Contains("ea app"))
        {
            slug = "ea";
            hex = "FF4747";
            return true;
        }

        if (Matches(process, "upc", "ubisoftconnect", "ubisoftgamelauncher") || name.Contains("ubisoft"))
        {
            slug = "ubisoft";
            hex = "FFFFFF";
            return true;
        }

        if (Matches(process, "riotclientservices", "riotclientux") || name.Contains("riot"))
        {
            slug = "riotgames";
            hex = "D32936";
            return true;
        }

        if (Matches(process, "twitch") || name.Contains("twitch"))
        {
            slug = "twitch";
            hex = "9146FF";
            return true;
        }

        return false;
    }

    private static ImageSource? TryFromExecutable(string? processName)
    {
        var path = FindExecutable(processName);
        if (path is null)
        {
            return null;
        }

        try
        {
            using var icon = DrawingIcon.ExtractAssociatedIcon(path);
            if (icon is null)
            {
                return null;
            }

            using var bitmap = icon.ToBitmap();
            return ToImageSource(bitmap);
        }
        catch
        {
            return null;
        }
    }

    private static string? FindExecutable(string? processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
        {
            return null;
        }

        var key = StripExe(processName);
        if (RememberedPaths.TryGetValue(key, out var remembered) && File.Exists(remembered))
        {
            return remembered;
        }

        var running = FindRunningExecutable(key);
        if (running is not null)
        {
            RememberedPaths[key] = running;
            return running;
        }

        foreach (var candidate in KnownInstallPaths(key))
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static string? FindRunningExecutable(string processName)
    {
        Process[] processes;
        try
        {
            processes = Process.GetProcessesByName(processName);
        }
        catch
        {
            return null;
        }

        foreach (var process in processes)
        {
            try
            {
                var path = process.MainModule?.FileName;
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                {
                    return path;
                }
            }
            catch
            {
                // Access denied for some processes; fall back to bundled icons.
            }
            finally
            {
                process.Dispose();
            }
        }

        return null;
    }

    private static IEnumerable<string> KnownInstallPaths(string processName)
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        if (processName.StartsWith("fivem", StringComparison.OrdinalIgnoreCase))
        {
            yield return Path.Combine(local, "FiveM", "FiveM.exe");
            yield return Path.Combine(local, "FiveM", "FiveM.app", "FiveM.exe");
        }

        if (processName.StartsWith("redm", StringComparison.OrdinalIgnoreCase))
        {
            yield return Path.Combine(local, "RedM", "RedM.exe");
        }

        if (processName.Contains("valorant", StringComparison.OrdinalIgnoreCase))
        {
            yield return Path.Combine(pf, "Riot Games", "VALORANT", "live", "VALORANT.exe");
            yield return @"C:\Riot Games\VALORANT\live\VALORANT.exe";
        }

        if (processName.Equals("cs2", StringComparison.OrdinalIgnoreCase))
        {
            yield return Path.Combine(pf86, "Steam", "steamapps", "common", "Counter-Strike Global Offensive", "game", "bin", "win64", "cs2.exe");
            yield return Path.Combine(pf, "Steam", "steamapps", "common", "Counter-Strike Global Offensive", "game", "bin", "win64", "cs2.exe");
        }

        if (processName.StartsWith("gta5", StringComparison.OrdinalIgnoreCase))
        {
            yield return Path.Combine(pf, "Rockstar Games", "Grand Theft Auto V", "GTA5.exe");
            yield return Path.Combine(pf86, "Steam", "steamapps", "common", "Grand Theft Auto V", "GTA5.exe");
        }

        if (processName.Contains("discord", StringComparison.OrdinalIgnoreCase))
        {
            yield return Path.Combine(local, "Discord", "Update.exe");
        }
    }

    private static ImageSource? LoadPackedPng(string slug)
    {
        try
        {
            var uri = new Uri($"pack://application:,,,/MicPilot;component/Assets/games/{slug}.png");
            var resource = System.Windows.Application.GetResourceStream(uri);
            if (resource is null)
            {
                return null;
            }

            using var stream = resource.Stream;
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            return null;
        }
    }

    private static ImageSource ToImageSource(Bitmap bitmap)
    {
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        stream.Position = 0;

        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }

    private static bool Matches(string processName, params string[] tokens)
    {
        if (string.IsNullOrEmpty(processName))
        {
            return false;
        }

        foreach (var token in tokens)
        {
            if (processName.Equals(token, StringComparison.OrdinalIgnoreCase) ||
                processName.StartsWith(token + "_", StringComparison.OrdinalIgnoreCase) ||
                processName.StartsWith(token + "-", StringComparison.OrdinalIgnoreCase) ||
                (token.Length >= 4 && processName.Contains(token, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }

    private static string StripExe(string? processName)
    {
        var value = (processName ?? "").Trim();
        return value.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? value[..^4]
            : value;
    }

    private static string PathsFile =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MicPilot",
            "exe-paths.json");

    private static void LoadRememberedPaths()
    {
        try
        {
            var file = PathsFile;
            if (!File.Exists(file))
            {
                return;
            }

            var json = File.ReadAllText(file);
            var map = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (map is null)
            {
                return;
            }

            foreach (var pair in map)
            {
                if (!string.IsNullOrWhiteSpace(pair.Key) && File.Exists(pair.Value))
                {
                    RememberedPaths[pair.Key] = pair.Value;
                }
            }
        }
        catch
        {
            // Ignore corrupt cache.
        }
    }

    private static void SaveRememberedPaths()
    {
        try
        {
            var file = PathsFile;
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            lock (PathFileLock)
            {
                var snapshot = RememberedPaths.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
                File.WriteAllText(file, JsonSerializer.Serialize(snapshot, JsonOptions));
            }
        }
        catch
        {
            // Non-fatal.
        }
    }

}
