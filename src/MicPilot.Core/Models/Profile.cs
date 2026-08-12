namespace MicPilot.Core.Models;

public sealed class Profile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string ProcessName { get; set; } = string.Empty;

    public string Hotkey { get; set; } = "PgDn";

    public HotkeyMode Mode { get; set; } = HotkeyMode.Toggle;

    public bool AutoActivate { get; set; }

    public bool Enabled { get; set; } = true;
}
