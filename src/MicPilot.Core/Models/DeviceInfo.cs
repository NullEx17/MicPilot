namespace MicPilot.Core.Models;

public sealed class DeviceInfo
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public DeviceConnectionState State { get; set; } = DeviceConnectionState.Active;
}

public enum DeviceConnectionState
{
    Active,
    Disabled,
    NotPresent,
    Unplugged
}
