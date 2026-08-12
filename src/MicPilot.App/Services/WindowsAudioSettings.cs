using System.Diagnostics;
using MicPilot.Diagnostics;

namespace MicPilot.App.Services;

public static class WindowsAudioSettings
{
    public static void Open()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "ms-settings:sound",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Log.Warn($"Could not open Windows sound settings: {ex.Message}");
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "control.exe",
                    Arguments = "mmsys.cpl,,1",
                    UseShellExecute = true
                });
            }
            catch (Exception fallbackEx)
            {
                Log.Error("Could not open classic Sound control panel.", fallbackEx);
            }
        }
    }
}
