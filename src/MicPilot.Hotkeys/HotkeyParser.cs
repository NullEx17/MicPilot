namespace MicPilot.Hotkeys;

public static class HotkeyParser
{
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint ModWin = 0x0008;

    private static readonly Dictionary<string, uint> Keys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PgDn"] = 0x22,
        ["PageDown"] = 0x22,
        ["PgUp"] = 0x21,
        ["PageUp"] = 0x21,
        ["Home"] = 0x24,
        ["End"] = 0x23,
        ["Insert"] = 0x2D,
        ["Ins"] = 0x2D,
        ["Delete"] = 0x2E,
        ["Del"] = 0x2E,
        ["F1"] = 0x70,
        ["F2"] = 0x71,
        ["F3"] = 0x72,
        ["F4"] = 0x73,
        ["F5"] = 0x74,
        ["F6"] = 0x75,
        ["F7"] = 0x76,
        ["F8"] = 0x77,
        ["F9"] = 0x78,
        ["F10"] = 0x79,
        ["F11"] = 0x7A,
        ["F12"] = 0x7B,
        ["Space"] = 0x20,
        ["Pause"] = 0x13,
        ["ScrollLock"] = 0x91,
    };

    public static readonly string[] SuggestedHotkeys =
    [
        "PgDn",
        "Home",
        "Insert",
        "Delete",
        "F9",
        "F10",
        "F11",
        "F12",
        "Ctrl+PgDn",
        "Alt+PgDn",
        "Shift+PgDn",
        "Ctrl+Home",
        "Alt+Home"
    ];

    public static bool TryParse(string? text, out HotkeyDefinition definition)
    {
        definition = null!;

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var parts = text
            .Split(['+', '-'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 0)
        {
            return false;
        }

        uint modifiers = 0;
        string? keyName = null;

        foreach (var part in parts)
        {
            if (IsModifier(part, out var modifier))
            {
                modifiers |= modifier;
                continue;
            }

            if (keyName is not null)
            {
                return false;
            }

            keyName = CanonicalKeyName(part);
        }

        if (keyName is null || !Keys.TryGetValue(keyName, out var virtualKey))
        {
            return false;
        }

        // Reject modifier-only combos and bare Win key usage for safety.
        if ((modifiers & ModWin) != 0)
        {
            return false;
        }

        definition = new HotkeyDefinition(modifiers, virtualKey, FormatDisplay(modifiers, keyName));
        return true;
    }

    public static bool IsKeyDown(uint virtualKey) =>
        (NativeMethods.GetAsyncKeyState((int)virtualKey) & 0x8000) != 0;

    public static bool AreModifiersDown(uint modifiers)
    {
        if ((modifiers & ModControl) != 0 && !IsKeyDown(0x11))
        {
            return false;
        }

        if ((modifiers & ModAlt) != 0 && !IsKeyDown(0x12))
        {
            return false;
        }

        if ((modifiers & ModShift) != 0 && !IsKeyDown(0x10))
        {
            return false;
        }

        return true;
    }

    private static bool IsModifier(string part, out uint modifier)
    {
        switch (part.ToLowerInvariant())
        {
            case "ctrl":
            case "control":
                modifier = ModControl;
                return true;
            case "alt":
                modifier = ModAlt;
                return true;
            case "shift":
                modifier = ModShift;
                return true;
            case "win":
            case "windows":
                modifier = ModWin;
                return true;
            default:
                modifier = 0;
                return false;
        }
    }

    private static string CanonicalKeyName(string part)
    {
        foreach (var key in Keys.Keys)
        {
            if (!string.Equals(key, part, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return key switch
            {
                "PageDown" => "PgDn",
                "PageUp" => "PgUp",
                "Ins" => "Insert",
                "Del" => "Delete",
                "pgdn" => "PgDn",
                _ => key
            };
        }

        return part;
    }

    private static string FormatDisplay(uint modifiers, string keyName)
    {
        var parts = new List<string>();
        if ((modifiers & ModControl) != 0)
        {
            parts.Add("Ctrl");
        }

        if ((modifiers & ModAlt) != 0)
        {
            parts.Add("Alt");
        }

        if ((modifiers & ModShift) != 0)
        {
            parts.Add("Shift");
        }

        parts.Add(keyName);
        return string.Join("+", parts);
    }
}
