namespace MicPilot.Core.Models;

public sealed class AppSettings
{
    public string InputDeviceId { get; set; } = string.Empty;

    public string VirtualOutputDeviceId { get; set; } = string.Empty;

    public string VirtualRenderDeviceId { get; set; } = string.Empty;

    public int SampleRate { get; set; } = 48000;

    public string GlobalHotkey { get; set; } = "PgDn";

    public HotkeyMode HotkeyMode { get; set; } = HotkeyMode.Toggle;

    public bool StartWithWindows { get; set; }

    public bool StartMinimized { get; set; }

    public bool MinimizeToTray { get; set; } = true;

    public bool NotificationsEnabled { get; set; } = true;

    public bool NotifyOnMuteToggle { get; set; }

    public bool NotifyOnGameDetected { get; set; } = true;

    public bool SoundFeedbackEnabled { get; set; }

    public bool HasCompletedFirstRun { get; set; }

    public bool SetupTipDismissed { get; set; }

    public bool OverlayEnabled { get; set; } = true;

    public bool OverlayShowOnChange { get; set; } = true;

    public bool OverlayAlwaysVisible { get; set; }

    public OverlayPosition OverlayPosition { get; set; } = OverlayPosition.TopRight;

    public double OverlayDurationSeconds { get; set; } = 2.0;

    public double OverlayOpacity { get; set; } = 0.92;

    public OverlaySize OverlaySize { get; set; } = OverlaySize.Small;

    public Guid? ActiveProfileId { get; set; }

    public List<Profile> Profiles { get; set; } = [];
}
