using MicPilot.App.Services;
using MicPilot.Audio;
using MicPilot.Core.Models;
using MicPilot.Core.Settings;
using MicPilot.Hotkeys;

namespace MicPilot.App.ViewModels;

public sealed class FirstRunViewModel : ViewModelBase
{
    private readonly AppSettings _settings;
    private AudioEndpoint? _selectedInputDevice;
    private string _hotkey;

    public FirstRunViewModel(
        AppSettings settings,
        IReadOnlyList<AudioEndpoint> captureDevices,
        VbCablePair? vbCable)
    {
        _settings = settings;
        CaptureDevices = captureDevices;
        VbCable = vbCable;
        _hotkey = settings.GlobalHotkey;

        _selectedInputDevice = captureDevices.FirstOrDefault(d => d.Id == settings.InputDeviceId)
                               ?? captureDevices.FirstOrDefault(d => d.IsDefault)
                               ?? captureDevices.FirstOrDefault();
    }

    public IReadOnlyList<AudioEndpoint> CaptureDevices { get; }

    public VbCablePair? VbCable { get; }

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

    public string Hotkey
    {
        get => _hotkey;
        set => SetProperty(ref _hotkey, value);
    }

    public string VirtualMicName => VbCable?.GameMicrophoneName ?? "VB-CABLE not detected";

    public string SetupInstruction =>
        VbCable is null
            ? "Install VB-Audio Virtual Cable, then restart MicPilot."
            : $"Set your game's microphone to: {VirtualMicName}";

    public bool Save()
    {
        if (SelectedInputDevice is null)
        {
            return false;
        }

        if (!HotkeyParser.TryParse(Hotkey, out var hotkey))
        {
            return false;
        }

        _settings.InputDeviceId = SelectedInputDevice.Id;
        _settings.GlobalHotkey = hotkey.DisplayName;
        if (_settings.ActiveProfileId is Guid id)
        {
            var profile = _settings.Profiles.FirstOrDefault(p => p.Id == id);
            if (profile is not null)
            {
                profile.Hotkey = hotkey.DisplayName;
            }
        }

        _settings.HasCompletedFirstRun = true;
        SettingsStore.Save(_settings);
        return true;
    }
}
