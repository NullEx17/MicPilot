namespace MicPilot.Audio;

public static class VbCableDetector
{
    public static bool IsCableInput(string deviceName) =>
        deviceName.Contains("CABLE Input", StringComparison.OrdinalIgnoreCase) &&
        deviceName.Contains("VB-Audio", StringComparison.OrdinalIgnoreCase);

    public static bool IsCableOutput(string deviceName) =>
        deviceName.Contains("CABLE Output", StringComparison.OrdinalIgnoreCase) &&
        deviceName.Contains("VB-Audio", StringComparison.OrdinalIgnoreCase);

    public static bool IsVirtualCable(string deviceName) =>
        IsCableInput(deviceName) || IsCableOutput(deviceName);
}
