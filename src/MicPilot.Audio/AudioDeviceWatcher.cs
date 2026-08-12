using MicPilot.Diagnostics;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace MicPilot.Audio;

/// <summary>
/// Listens for WASAPI endpoint add/remove/state/default changes.
/// Callbacks arrive on a COM/audio thread — marshal before touching UI or restarting audio.
/// </summary>
public sealed class AudioDeviceWatcher : IMMNotificationClient, IDisposable
{
    private readonly MMDeviceEnumerator _enumerator = new();
    private bool _registered;
    private bool _disposed;

    public event Action<string>? DevicesChanged;

    public void Start()
    {
        if (_registered)
        {
            return;
        }

        _enumerator.RegisterEndpointNotificationCallback(this);
        _registered = true;
        Log.Info("Audio device watcher started");
    }

    public void OnDeviceStateChanged(string deviceId, DeviceState newState)
    {
        Log.Info($"Device state changed: {deviceId} -> {newState}");
        RaiseChanged($"state:{newState}");
    }

    public void OnDeviceAdded(string deviceId)
    {
        Log.Info($"Device added: {deviceId}");
        RaiseChanged("added");
    }

    public void OnDeviceRemoved(string deviceId)
    {
        Log.Info($"Device removed: {deviceId}");
        RaiseChanged("removed");
    }

    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
    {
        // Observed only — MicPilot does not hijack the Windows default microphone.
        Log.Info($"Default {flow}/{role} device changed -> {defaultDeviceId}");
        RaiseChanged($"default:{flow}");
    }

    public void OnPropertyValueChanged(string deviceId, PropertyKey key)
    {
        // Ignore noisy property spam; state/add/remove/default are enough for recovery.
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_registered)
        {
            try
            {
                _enumerator.UnregisterEndpointNotificationCallback(this);
            }
            catch
            {
                // Best effort.
            }

            _registered = false;
        }

        _enumerator.Dispose();
    }

    private void RaiseChanged(string reason) => DevicesChanged?.Invoke(reason);
}
