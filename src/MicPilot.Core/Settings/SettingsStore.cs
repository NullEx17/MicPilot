using System.Text.Json;
using System.Text.Json.Serialization;
using MicPilot.Core.Models;

namespace MicPilot.Core.Settings;

public static class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static string SettingsPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MicPilot",
            "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return CreateDefault();
            }

            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? CreateDefault();

            // Existing installs already configured a mic before first-run existed.
            if (!settings.HasCompletedFirstRun && !string.IsNullOrWhiteSpace(settings.InputDeviceId))
            {
                settings.HasCompletedFirstRun = true;
                settings.SetupTipDismissed = true;
            }

            return settings;
        }
        catch
        {
            return CreateDefault();
        }
    }

    public static void Save(AppSettings settings)
    {
        var directory = Path.GetDirectoryName(SettingsPath)!;
        Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(SettingsPath, json);
    }

    public static AppSettings CreateDefault()
    {
        var fiveM = new Profile
        {
            Name = "FiveM",
            ProcessName = "FiveM_GTAProcess.exe",
            Hotkey = "PgDn",
            AutoActivate = true,
            Enabled = true
        };

        var valorant = new Profile
        {
            Name = "Valorant",
            ProcessName = "VALORANT-Win64-Shipping.exe",
            Hotkey = "Home",
            Enabled = true
        };

        return new AppSettings
        {
            ActiveProfileId = fiveM.Id,
            Profiles = [fiveM, valorant]
        };
    }
}
