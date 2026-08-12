using System.Windows.Input;
using System.Windows.Threading;
using MicPilot.App.Services;
using MicPilot.Audio;
using MicPilot.Core.Models;
using MicPilot.Core.Settings;
using MicPilot.Diagnostics;
using MicPilot.Hotkeys;
using MicPilot.Profiles;
using Microsoft.Win32;

namespace MicPilot.App.ViewModels;

public sealed class MainViewModel : ViewModelBase, IDisposable
{
    private readonly ProcessWatcher _processWatcher = new();
    private readonly AudioDeviceCatalog _deviceCatalog = new();
    private readonly AudioDeviceWatcher _deviceWatcher = new();
    private readonly MicRoutingEngine _routingEngine = new();
    private readonly GlobalHotkeyService _hotkeys = new();
    private readonly DispatcherTimer _uiTimer;
    private readonly DispatcherTimer _walkieTimer;
    private readonly DispatcherTimer _recoveryTimer;
    private readonly AppSettings _settings;

    private Profile? _activeProfile;
    private GameMicState _gameMicState = GameMicState.On;
    private string _inputDeviceName = "Not configured";
    private string _virtualOutputName = "Not configured";
    private double _inputLevel;
    private double _displayLevel;
    private bool _audioEngineRunning;
    private bool _virtualDeviceAvailable;
    private bool _needsRecovery;
    private string _statusMessage = "Ready";
    private double _estimatedLatencyMs;
    private string _hotkeyStatus = "Hotkey not registered";
    private bool _walkieHolding;
    private HotkeyDefinition? _activeHotkey;
    private string? _pendingRecoveryReason;
    private bool _lastGameRunning;

    public MainViewModel()
    {
        _settings = SettingsStore.Load();

        _routingEngine.InputLevelChanged += level =>
        {
            _inputLevel = level;
        };

        _routingEngine.ErrorOccurred += message =>
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                AudioEngineRunning = false;
                NeedsRecovery = true;
                StatusMessage = message;
                NotificationRequested?.Invoke("MicPilot", message);
            });
        };

        _hotkeys.Pressed += () =>
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(OnHotkeyPressed);
        };

        _deviceWatcher.DevicesChanged += reason =>
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() => ScheduleRecovery(reason));
        };

        SystemEvents.PowerModeChanged += OnPowerModeChanged;

        _uiTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(33)
        };
        _uiTimer.Tick += (_, _) => OnUiTick();
        _uiTimer.Start();

        _walkieTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(30)
        };
        _walkieTimer.Tick += (_, _) => PollWalkieRelease();

        _recoveryTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(700)
        };
        _recoveryTimer.Tick += (_, _) =>
        {
            _recoveryTimer.Stop();
            RecoverAudio(_pendingRecoveryReason ?? "device-change");
        };

        ActiveProfile = ResolveActiveProfile();

        ToggleGameMicCommand = new RelayCommand(ToggleGameMic, () => _audioEngineRunning);
        OpenProfilesCommand = new RelayCommand(() => OpenProfilesRequested?.Invoke());
        OpenSettingsCommand = new RelayCommand(() => OpenSettingsRequested?.Invoke());
        RetryAudioCommand = new RelayCommand(() => RecoverAudio("manual-retry"));
        OpenAudioSettingsCommand = new RelayCommand(WindowsAudioSettings.Open);
        DismissSetupTipCommand = new RelayCommand(DismissSetupTip);

        _deviceWatcher.Start();
        RefreshDevicesAndStart();
    }

    public event Action? OpenProfilesRequested;
    public event Action? OpenSettingsRequested;
    public event Action? GameMicStateChanged;
    public event Action<string, string>? NotificationRequested;

    public AppSettings Settings => _settings;

    public ICommand ToggleGameMicCommand { get; }
    public ICommand OpenProfilesCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand RetryAudioCommand { get; }
    public ICommand OpenAudioSettingsCommand { get; }
    public ICommand DismissSetupTipCommand { get; }

    public bool ShowSetupTip => !_settings.SetupTipDismissed;

    public Profile? ActiveProfile
    {
        get => _activeProfile;
        private set
        {
            if (SetProperty(ref _activeProfile, value))
            {
                OnPropertyChanged(nameof(ActiveProfileName));
                OnPropertyChanged(nameof(ActiveProfileProcess));
                OnPropertyChanged(nameof(ActiveProfileHotkey));
                OnPropertyChanged(nameof(IsGameRunning));
                OnPropertyChanged(nameof(GameRunningText));
                OnPropertyChanged(nameof(ActiveHotkeyModeText));
                OnPropertyChanged(nameof(GameMicHintText));
                OnPropertyChanged(nameof(ActiveProfileIcon));
                OnPropertyChanged(nameof(HasActiveProfileIcon));
            }
        }
    }

    public System.Windows.Media.ImageSource? ActiveProfileIcon =>
        GameIconResolver.Resolve(ActiveProfile?.ProcessName, ActiveProfile?.Name);

    public bool HasActiveProfileIcon => ActiveProfileIcon is not null;

    public string ActiveProfileName => ActiveProfile?.Name ?? "No profile";

    public string ActiveProfileProcess => ActiveProfile?.ProcessName ?? "—";

    public string ActiveProfileHotkey => ActiveProfile?.Hotkey ?? _settings.GlobalHotkey;

    public string ActiveHotkeyModeText
    {
        get
        {
            var mode = ActiveProfile?.Mode ?? _settings.HotkeyMode;
            return mode == HotkeyMode.WalkieTalkie ? "Hold to talk (game)" : "Toggle";
        }
    }

    public bool IsGameRunning =>
        ActiveProfile is not null && _processWatcher.IsProcessRunning(ActiveProfile.ProcessName);

    public string GameRunningText => IsGameRunning ? "Running" : "Not running";

    public GameMicState GameMicState
    {
        get => _gameMicState;
        private set
        {
            if (SetProperty(ref _gameMicState, value))
            {
                OnPropertyChanged(nameof(GameMicStateText));
                OnPropertyChanged(nameof(GameMicToggleText));
                OnPropertyChanged(nameof(GameMicHintText));
                GameMicStateChanged?.Invoke();
            }
        }
    }

    public string GameMicStateText => GameMicState == GameMicState.On ? "ON" : "MUTED";

    public string GameMicToggleText =>
        GameMicState == GameMicState.On ? "Mute Game Mic" : "Unmute Game Mic";

    public string GameMicHintText =>
        GameMicState == GameMicState.On
            ? $"Press {ActiveProfileHotkey} to mute game mic"
            : $"Press {ActiveProfileHotkey} to unmute game mic";

    public string InputDeviceName
    {
        get => _inputDeviceName;
        private set => SetProperty(ref _inputDeviceName, value);
    }

    public string VirtualOutputName
    {
        get => _virtualOutputName;
        private set => SetProperty(ref _virtualOutputName, value);
    }

    public double InputLevel
    {
        get => _displayLevel;
        private set => SetProperty(ref _displayLevel, value);
    }

    public bool AudioEngineRunning
    {
        get => _audioEngineRunning;
        private set
        {
            if (SetProperty(ref _audioEngineRunning, value))
            {
                OnPropertyChanged(nameof(AudioEngineStatusText));
                OnPropertyChanged(nameof(ShowRecoveryActions));
            }
        }
    }

    public bool VirtualDeviceAvailable
    {
        get => _virtualDeviceAvailable;
        private set
        {
            if (SetProperty(ref _virtualDeviceAvailable, value))
            {
                OnPropertyChanged(nameof(VirtualDeviceStatusText));
            }
        }
    }

    public bool NeedsRecovery
    {
        get => _needsRecovery;
        private set
        {
            if (SetProperty(ref _needsRecovery, value))
            {
                OnPropertyChanged(nameof(ShowRecoveryActions));
            }
        }
    }

    public bool ShowRecoveryActions => NeedsRecovery || !AudioEngineRunning;

    public string AudioEngineStatusText => AudioEngineRunning ? "Running" : "Stopped";

    public string VirtualDeviceStatusText => VirtualDeviceAvailable ? "Available" : "Unavailable";

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string HotkeyStatus
    {
        get => _hotkeyStatus;
        private set => SetProperty(ref _hotkeyStatus, value);
    }

    public double EstimatedLatencyMs
    {
        get => _estimatedLatencyMs;
        private set
        {
            if (SetProperty(ref _estimatedLatencyMs, value))
            {
                OnPropertyChanged(nameof(EstimatedLatencyText));
            }
        }
    }

    public string EstimatedLatencyText =>
        EstimatedLatencyMs > 0 ? $"Estimated latency: {EstimatedLatencyMs:F0} ms" : string.Empty;

    public IReadOnlyList<AudioEndpoint> GetPhysicalCaptureDevices() =>
        _deviceCatalog.GetCaptureDevices()
            .Where(device => !VbCableDetector.IsVirtualCable(device.Name))
            .Where(device => device.State == DeviceConnectionState.Active)
            .ToList();

    public VbCablePair? GetVbCable() => _deviceCatalog.FindVbCable();

    public void AttachHotkeys(IntPtr windowHandle)
    {
        if (!_hotkeys.Attach(windowHandle))
        {
            HotkeyStatus = "Hotkey unavailable";
            return;
        }

        RegisterActiveHotkey();
    }

    public void ApplySettingsAndRestart()
    {
        SettingsStore.Save(_settings);
        RefreshDevicesAndStart();
        RegisterActiveHotkey();
    }

    public void ApplyProfiles()
    {
        ActiveProfile = ResolveActiveProfile();
        RegisterActiveHotkey();
        GameMicStateChanged?.Invoke();
    }

    public void Dispose()
    {
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        _uiTimer.Stop();
        _walkieTimer.Stop();
        _recoveryTimer.Stop();
        _deviceWatcher.Dispose();
        _hotkeys.Dispose();
        _routingEngine.Dispose();
        _deviceCatalog.Dispose();
    }

    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode != PowerModes.Resume)
        {
            return;
        }

        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            ScheduleRecovery("resume"));
    }

    private void ScheduleRecovery(string reason)
    {
        _pendingRecoveryReason = reason;
        NeedsRecovery = true;
        _recoveryTimer.Stop();
        _recoveryTimer.Start();
    }

    private void RecoverAudio(string reason)
    {
        Log.Info($"Audio recovery started ({reason})");
        StatusMessage = "Reconnecting audio devices…";
        RefreshDevicesAndStart();

        if (AudioEngineRunning)
        {
            NeedsRecovery = false;
            NotificationRequested?.Invoke("MicPilot", "Audio devices reconnected");
        }
    }

    private void OnUiTick()
    {
        InputLevel = Math.Max(_inputLevel, _displayLevel * 0.86);

        var previousId = ActiveProfile?.Id;
        var wasRunning = _lastGameRunning;
        ActiveProfile = ResolveActiveProfile();
        if (ActiveProfile?.Id != previousId)
        {
            RegisterActiveHotkey();
            GameMicStateChanged?.Invoke();
        }

        OnPropertyChanged(nameof(IsGameRunning));
        OnPropertyChanged(nameof(GameRunningText));

        if (IsGameRunning != wasRunning)
        {
            _lastGameRunning = IsGameRunning;
            OnPropertyChanged(nameof(ActiveProfileIcon));
            OnPropertyChanged(nameof(HasActiveProfileIcon));
        }
    }

    private void OnHotkeyPressed()
    {
        if (!AudioEngineRunning)
        {
            StatusMessage = "Audio engine is not running.";
            return;
        }

        var mode = ActiveProfile?.Mode ?? _settings.HotkeyMode;
        if (mode == HotkeyMode.WalkieTalkie)
        {
            BeginWalkieHold();
            return;
        }

        ToggleGameMic();
    }

    private void BeginWalkieHold()
    {
        _walkieHolding = true;
        SetGameMicState(GameMicState.On, notify: true);
        _walkieTimer.Start();
    }

    private void PollWalkieRelease()
    {
        if (!_walkieHolding || _activeHotkey is null)
        {
            _walkieTimer.Stop();
            return;
        }

        var keyDown = HotkeyParser.IsKeyDown(_activeHotkey.VirtualKey);
        var modifiersDown = HotkeyParser.AreModifiersDown(_activeHotkey.Modifiers);
        if (keyDown && modifiersDown)
        {
            return;
        }

        _walkieHolding = false;
        _walkieTimer.Stop();
        SetGameMicState(GameMicState.Off, notify: true);
    }

    private void RegisterActiveHotkey()
    {
        var hotkeyText = ActiveProfile?.Hotkey ?? _settings.GlobalHotkey;
        if (!HotkeyParser.TryParse(hotkeyText, out var definition))
        {
            HotkeyStatus = $"Invalid hotkey: {hotkeyText}";
            _hotkeys.Unregister();
            _activeHotkey = null;
            return;
        }

        if (_hotkeys.Register(definition))
        {
            _activeHotkey = definition;
            HotkeyStatus = $"Hotkey: {definition.DisplayName}";
            _settings.GlobalHotkey = definition.DisplayName;
        }
        else
        {
            _activeHotkey = null;
            HotkeyStatus = $"Could not register {definition.DisplayName} (already in use?)";
        }
    }

    private void RefreshDevicesAndStart()
    {
        ActiveProfile = ResolveActiveProfile();

        var vbCable = _deviceCatalog.FindVbCable();
        VirtualDeviceAvailable = vbCable is not null;

        if (vbCable is not null)
        {
            _settings.VirtualOutputDeviceId = vbCable.CaptureDeviceId;
            _settings.VirtualRenderDeviceId = vbCable.RenderDeviceId;
            VirtualOutputName = vbCable.GameMicrophoneName;
        }
        else
        {
            _routingEngine.Stop();
            VirtualOutputName = "VB-CABLE not found";
            AudioEngineRunning = false;
            NeedsRecovery = true;
            StatusMessage = "Virtual microphone not found. Install VB-CABLE from vb-audio.com.";
            Log.Warn("VB-CABLE not detected.");
            return;
        }

        var preferredStillPresent = !string.IsNullOrWhiteSpace(_settings.InputDeviceId) &&
            _deviceCatalog.GetCaptureDevices().Any(device =>
                device.Id == _settings.InputDeviceId &&
                device.State == DeviceConnectionState.Active);

        if (!preferredStillPresent && !string.IsNullOrWhiteSpace(_settings.InputDeviceId))
        {
            Log.Warn("Configured microphone is unavailable.");
            StatusMessage = "Microphone disconnected.";
            NeedsRecovery = true;
        }

        var inputDevice = _deviceCatalog.GetPreferredPhysicalCapture(_settings.InputDeviceId);
        if (inputDevice is null)
        {
            _routingEngine.Stop();
            InputDeviceName = "No microphone found";
            AudioEngineRunning = false;
            NeedsRecovery = true;
            StatusMessage = "Microphone disconnected.";
            return;
        }

        if (!preferredStillPresent && inputDevice.Id != _settings.InputDeviceId)
        {
            // Fall back without permanently overwriting the user's preferred device until Save.
            InputDeviceName = $"{inputDevice.Name} (temporary)";
            Log.Info($"Using temporary capture device '{inputDevice.Name}'");
        }
        else
        {
            _settings.InputDeviceId = inputDevice.Id;
            InputDeviceName = inputDevice.Name;
            SettingsStore.Save(_settings);
        }

        StartAudioEngine(inputDevice.Id);
    }

    private void StartAudioEngine(string? captureDeviceId = null)
    {
        var inputId = captureDeviceId ?? _settings.InputDeviceId;
        if (string.IsNullOrWhiteSpace(inputId) ||
            string.IsNullOrWhiteSpace(_settings.VirtualRenderDeviceId))
        {
            StatusMessage = "Select a microphone in Settings.";
            NeedsRecovery = true;
            return;
        }

        try
        {
            _routingEngine.Stop();
            _routingEngine.Start(inputId, _settings.VirtualRenderDeviceId);
            _routingEngine.SetRouteMuted(GameMicState == GameMicState.Off);

            AudioEngineRunning = _routingEngine.IsRunning;
            EstimatedLatencyMs = _routingEngine.EstimatedLatencyMs;
            NeedsRecovery = !AudioEngineRunning;
            StatusMessage = AudioEngineRunning
                ? $"Routing active. {EstimatedLatencyText}"
                : "Audio engine stopped.";
        }
        catch
        {
            AudioEngineRunning = false;
            NeedsRecovery = true;
            StatusMessage = "MicPilot couldn't access your microphone. Check Settings and retry.";
        }
    }

    private void ToggleGameMic() =>
        SetGameMicState(
            GameMicState == GameMicState.On ? GameMicState.Off : GameMicState.On,
            notify: true);

    private void SetGameMicState(GameMicState state, bool notify)
    {
        if (GameMicState == state)
        {
            _routingEngine.SetRouteMuted(state == GameMicState.Off);
            return;
        }

        GameMicState = state;
        _routingEngine.SetRouteMuted(state == GameMicState.Off);

        Log.Info($"Game mic set to {state} for profile '{ActiveProfileName}'");
        StatusMessage = state == GameMicState.On
            ? $"{ActiveProfileName} microphone enabled"
            : $"{ActiveProfileName} microphone muted";

        if (notify)
        {
            // Mute/unmute feedback defaults to the on-screen overlay; tray balloons are opt-in.
            if (_settings.NotifyOnMuteToggle)
            {
                NotificationRequested?.Invoke(
                    "MicPilot",
                    state == GameMicState.On
                        ? $"{ActiveProfileName} microphone enabled"
                        : $"{ActiveProfileName} microphone muted");
            }
        }
    }

    public void DismissSetupTip()
    {
        _settings.SetupTipDismissed = true;
        SettingsStore.Save(_settings);
        OnPropertyChanged(nameof(ShowSetupTip));
    }

    public void CompleteFirstRun()
    {
        _settings.HasCompletedFirstRun = true;
        SettingsStore.Save(_settings);
        OnPropertyChanged(nameof(ShowSetupTip));
    }

    private Profile? ResolveActiveProfile()
    {
        var autoProfile = _processWatcher.FindRunningAutoActivateProfile(_settings.Profiles);
        if (autoProfile is not null)
        {
            return autoProfile;
        }

        if (_settings.ActiveProfileId is Guid activeId)
        {
            return _settings.Profiles.FirstOrDefault(profile => profile.Id == activeId)
                   ?? _settings.Profiles.FirstOrDefault();
        }

        return _settings.Profiles.FirstOrDefault();
    }
}
