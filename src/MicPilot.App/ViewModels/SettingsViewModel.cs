using MicPilot.App.Services;
using MicPilot.Audio;
using MicPilot.Core.Models;
using MicPilot.Core.Settings;
using MicPilot.Hotkeys;

namespace MicPilot.App.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
    private readonly AppSettings _settings;
    private AudioEndpoint? _selectedInputDevice;
    private string _globalHotkey;
    private HotkeyMode _hotkeyMode;
    private bool _minimizeToTray;
    private bool _notificationsEnabled;
    private bool _notifyOnMuteToggle;
    private bool _notifyOnGameDetected;
    private bool _startMinimized;
    private bool _startWithWindows;
    private bool _overlayEnabled;
    private bool _overlayShowOnChange;
    private bool _overlayAlwaysVisible;
    private OverlayPosition _overlayPosition;
    private double _overlayDurationSeconds;
    private double _overlayOpacity;
    private OverlaySize _overlaySize;
    private bool _setupTipDismissed;

    public SettingsViewModel(AppSettings settings, IReadOnlyList<AudioEndpoint> captureDevices, VbCablePair? vbCable)
    {
        _settings = settings;
        CaptureDevices = captureDevices;
        VbCable = vbCable;

        _selectedInputDevice = captureDevices.FirstOrDefault(device => device.Id == settings.InputDeviceId)
                               ?? captureDevices.FirstOrDefault(device => device.IsDefault)
                               ?? captureDevices.FirstOrDefault();

        _globalHotkey = settings.GlobalHotkey;
        _hotkeyMode = settings.HotkeyMode;
        _minimizeToTray = settings.MinimizeToTray;
        _notificationsEnabled = settings.NotificationsEnabled;
        _notifyOnMuteToggle = settings.NotifyOnMuteToggle;
        _notifyOnGameDetected = settings.NotifyOnGameDetected;
        _startMinimized = settings.StartMinimized;
        _startWithWindows = settings.StartWithWindows || StartupHelper.IsEnabled();
        _overlayEnabled = settings.OverlayEnabled;
        _overlayShowOnChange = settings.OverlayShowOnChange;
        _overlayAlwaysVisible = settings.OverlayAlwaysVisible;
        _overlayPosition = settings.OverlayPosition;
        _overlayDurationSeconds = settings.OverlayDurationSeconds;
        _overlayOpacity = settings.OverlayOpacity;
        _overlaySize = settings.OverlaySize;
        _setupTipDismissed = settings.SetupTipDismissed;
    }

    public IReadOnlyList<AudioEndpoint> CaptureDevices { get; }
    public VbCablePair? VbCable { get; }
    public IReadOnlyList<HotkeyMode> ModeOptions { get; } = [HotkeyMode.Toggle, HotkeyMode.WalkieTalkie];
    public IReadOnlyList<OverlayPosition> PositionOptions { get; } =
        [OverlayPosition.TopRight, OverlayPosition.TopLeft, OverlayPosition.BottomRight, OverlayPosition.BottomLeft];
    public IReadOnlyList<OverlaySize> SizeOptions { get; } = [OverlaySize.Small, OverlaySize.Medium];

    public AudioEndpoint? SelectedInputDevice
    {
        get => _selectedInputDevice;
        set
        {
            if (SetProperty(ref _selectedInputDevice, value) && value is not null)
            {
                _settings.InputDeviceId = value.Id;
            }
        }
    }

    public string GlobalHotkey
    {
        get => _globalHotkey;
        set => SetProperty(ref _globalHotkey, value);
    }

    public HotkeyMode HotkeyMode
    {
        get => _hotkeyMode;
        set => SetProperty(ref _hotkeyMode, value);
    }

    public bool MinimizeToTray
    {
        get => _minimizeToTray;
        set => SetProperty(ref _minimizeToTray, value);
    }

    public bool NotificationsEnabled
    {
        get => _notificationsEnabled;
        set => SetProperty(ref _notificationsEnabled, value);
    }

    public bool NotifyOnMuteToggle
    {
        get => _notifyOnMuteToggle;
        set => SetProperty(ref _notifyOnMuteToggle, value);
    }

    public bool NotifyOnGameDetected
    {
        get => _notifyOnGameDetected;
        set => SetProperty(ref _notifyOnGameDetected, value);
    }

    public bool StartMinimized
    {
        get => _startMinimized;
        set => SetProperty(ref _startMinimized, value);
    }

    public bool StartWithWindows
    {
        get => _startWithWindows;
        set => SetProperty(ref _startWithWindows, value);
    }

    public bool OverlayEnabled
    {
        get => _overlayEnabled;
        set => SetProperty(ref _overlayEnabled, value);
    }

    public bool OverlayShowOnChange
    {
        get => _overlayShowOnChange;
        set => SetProperty(ref _overlayShowOnChange, value);
    }

    public bool OverlayAlwaysVisible
    {
        get => _overlayAlwaysVisible;
        set => SetProperty(ref _overlayAlwaysVisible, value);
    }

    public OverlayPosition OverlayPosition
    {
        get => _overlayPosition;
        set => SetProperty(ref _overlayPosition, value);
    }

    public double OverlayDurationSeconds
    {
        get => _overlayDurationSeconds;
        set => SetProperty(ref _overlayDurationSeconds, value);
    }

    public double OverlayOpacity
    {
        get => _overlayOpacity;
        set => SetProperty(ref _overlayOpacity, value);
    }

    public OverlaySize OverlaySize
    {
        get => _overlaySize;
        set => SetProperty(ref _overlaySize, value);
    }

    public bool SetupTipDismissed
    {
        get => _setupTipDismissed;
        set => SetProperty(ref _setupTipDismissed, value);
    }

    public string VirtualMicrophoneName => VbCable?.GameMicrophoneName ?? "VB-CABLE not detected";
    public string MicPilotOutputName => VbCable?.MicPilotOutputName ?? "—";
    public string SampleRateText => $"{_settings.SampleRate / 1000} kHz";
    public string VersionText => typeof(SettingsViewModel).Assembly.GetName().Version?.ToString(3) ?? "0.1.0";
    public string? SaveError { get; private set; }

    public bool Save()
    {
        SaveError = null;

        if (!HotkeyParser.TryParse(GlobalHotkey, out var hotkey))
        {
            SaveError = "That hotkey isn't valid. Try PgDn, Home, F9, or Ctrl+PgDn.";
            return false;
        }

        try
        {
            StartupHelper.SetEnabled(StartWithWindows, StartupHelper.GetExecutablePath());
        }
        catch
        {
            SaveError = "MicPilot couldn't update the Start with Windows setting.";
            return false;
        }

        _settings.GlobalHotkey = hotkey.DisplayName;
        _settings.HotkeyMode = HotkeyMode;
        _settings.MinimizeToTray = MinimizeToTray;
        _settings.NotificationsEnabled = NotificationsEnabled;
        _settings.NotifyOnMuteToggle = NotifyOnMuteToggle;
        _settings.NotifyOnGameDetected = NotifyOnGameDetected;
        _settings.StartMinimized = StartMinimized;
        _settings.StartWithWindows = StartWithWindows;
        _settings.OverlayEnabled = OverlayEnabled;
        _settings.OverlayShowOnChange = OverlayShowOnChange;
        _settings.OverlayAlwaysVisible = OverlayAlwaysVisible;
        _settings.OverlayPosition = OverlayPosition;
        _settings.OverlayDurationSeconds = Math.Clamp(OverlayDurationSeconds, 0.5, 10);
        _settings.OverlayOpacity = Math.Clamp(OverlayOpacity, 0.4, 1.0);
        _settings.OverlaySize = OverlaySize;
        _settings.SetupTipDismissed = SetupTipDismissed;

        if (_settings.ActiveProfileId is Guid activeId)
        {
            var active = _settings.Profiles.FirstOrDefault(profile => profile.Id == activeId);
            if (active is not null && string.IsNullOrWhiteSpace(active.Hotkey))
            {
                active.Hotkey = hotkey.DisplayName;
            }
        }

        SettingsStore.Save(_settings);
        return true;
    }
}
