namespace MicPilot.Hotkeys;

public sealed record HotkeyDefinition(uint Modifiers, uint VirtualKey, string DisplayName)
{
    public override string ToString() => DisplayName;
}
