using System.IO;
using Microsoft.Win32;
using MicPilot.Diagnostics;

namespace MicPilot.App.Services;

public static class StartupHelper
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "MicPilot";

    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            return key?.GetValue(ValueName) is string;
        }
        catch (Exception ex)
        {
            Log.Warn($"Could not read startup setting: {ex.Message}");
            return false;
        }
    }

    public static void SetEnabled(bool enabled, string executablePath)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath);

            if (key is null)
            {
                throw new InvalidOperationException("Could not open Windows Run registry key.");
            }

            if (enabled)
            {
                if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
                {
                    throw new FileNotFoundException("MicPilot executable path is missing.", executablePath);
                }

                key.SetValue(ValueName, $"\"{executablePath}\"");
                Log.Info("Enabled start with Windows");
            }
            else
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
                Log.Info("Disabled start with Windows");
            }
        }
        catch (Exception ex)
        {
            Log.Error("Failed to update start with Windows.", ex);
            throw;
        }
    }

    public static string GetExecutablePath()
    {
        var path = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            return path;
        }

        return Path.Combine(AppContext.BaseDirectory, "MicPilot.exe");
    }
}
