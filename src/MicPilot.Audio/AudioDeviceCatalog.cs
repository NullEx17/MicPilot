using MicPilot.Core.Models;
using MicPilot.Diagnostics;
using NAudio.CoreAudioApi;

namespace MicPilot.Audio;

public sealed class AudioDeviceCatalog : IDisposable
{
    private readonly MMDeviceEnumerator _enumerator = new();

    public IReadOnlyList<AudioEndpoint> GetCaptureDevices() =>
        Enumerate(DataFlow.Capture);

    public IReadOnlyList<AudioEndpoint> GetRenderDevices() =>
        Enumerate(DataFlow.Render);

    public AudioEndpoint? GetDefaultCapture()
    {
        try
        {
            using var device = _enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);
            return ToEndpoint(device, isDefault: true);
        }
        catch (Exception ex)
        {
            Log.Warn($"Default capture device unavailable: {ex.Message}");
            return null;
        }
    }

    public AudioEndpoint? GetPreferredPhysicalCapture(string? preferredDeviceId)
    {
        var captureDevices = GetCaptureDevices()
            .Where(device => device.State == DeviceConnectionState.Active)
            .Where(device => !VbCableDetector.IsVirtualCable(device.Name))
            .ToList();

        if (!string.IsNullOrWhiteSpace(preferredDeviceId))
        {
            var preferred = captureDevices.FirstOrDefault(device => device.Id == preferredDeviceId);
            if (preferred is not null)
            {
                return preferred;
            }
        }

        return captureDevices.FirstOrDefault(device => device.IsDefault)
               ?? captureDevices.FirstOrDefault()
               ?? GetDefaultCapture();
    }

    public VbCablePair? FindVbCable()
    {
        var cableInput = GetRenderDevices()
            .FirstOrDefault(device =>
                device.State == DeviceConnectionState.Active &&
                VbCableDetector.IsCableInput(device.Name));

        var cableOutput = GetCaptureDevices()
            .FirstOrDefault(device =>
                device.State == DeviceConnectionState.Active &&
                VbCableDetector.IsCableOutput(device.Name));

        if (cableInput is null || cableOutput is null)
        {
            return null;
        }

        return new VbCablePair(cableInput, cableOutput);
    }

    public AudioEndpoint? FindById(string deviceId, DataFlow flow)
    {
        var devices = flow == DataFlow.Capture ? GetCaptureDevices() : GetRenderDevices();
        return devices.FirstOrDefault(device => device.Id == deviceId);
    }

    public void Dispose() => _enumerator.Dispose();

    private IReadOnlyList<AudioEndpoint> Enumerate(DataFlow flow)
    {
        try
        {
            var defaultId = GetDefaultDeviceId(flow);
            var collection = _enumerator.EnumerateAudioEndPoints(flow, DeviceState.All);

            var endpoints = new List<AudioEndpoint>(collection.Count);
            for (var i = 0; i < collection.Count; i++)
            {
                using var device = collection[i];
                endpoints.Add(ToEndpoint(device, device.ID == defaultId));
            }

            return endpoints
                .OrderByDescending(device => device.IsDefault)
                .ThenBy(device => device.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (Exception ex)
        {
            Log.Error("Failed to enumerate audio devices.", ex);
            return [];
        }
    }

    private string? GetDefaultDeviceId(DataFlow flow)
    {
        try
        {
            using var device = _enumerator.GetDefaultAudioEndpoint(flow, Role.Multimedia);
            return device.ID;
        }
        catch
        {
            return null;
        }
    }

    private static AudioEndpoint ToEndpoint(MMDevice device, bool isDefault)
    {
        DeviceConnectionState state = device.State switch
        {
            DeviceState.Active => DeviceConnectionState.Active,
            DeviceState.Disabled => DeviceConnectionState.Disabled,
            DeviceState.NotPresent => DeviceConnectionState.NotPresent,
            DeviceState.Unplugged => DeviceConnectionState.Unplugged,
            _ => DeviceConnectionState.NotPresent
        };

        return new AudioEndpoint(device.ID, device.FriendlyName, state, isDefault);
    }
}
