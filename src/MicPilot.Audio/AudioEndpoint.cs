using MicPilot.Core.Models;

namespace MicPilot.Audio;

public sealed record AudioEndpoint(
    string Id,
    string Name,
    DeviceConnectionState State,
    bool IsDefault);

public sealed record VbCablePair(
    AudioEndpoint RenderEndpoint,
    AudioEndpoint CaptureEndpoint)
{
    public string RenderDeviceId => RenderEndpoint.Id;

    public string CaptureDeviceId => CaptureEndpoint.Id;

    public string GameMicrophoneName => CaptureEndpoint.Name;

    public string MicPilotOutputName => RenderEndpoint.Name;
}
