using MicPilot.Hotkeys;

namespace MicPilot.Core.Tests;

public class HotkeyParserTests
{
    [Theory]
    [InlineData("PgDn", true)]
    [InlineData("Ctrl+PgDn", true)]
    [InlineData("Alt+Home", true)]
    [InlineData("Shift+F9", true)]
    [InlineData("Ctrl+Alt+F12", true)]
    [InlineData("", false)]
    [InlineData("Win+PgDn", false)]
    [InlineData("Ctrl", false)]
    [InlineData("Banana", false)]
    public void TryParse_ValidatesInput(string text, bool expected)
    {
        var ok = HotkeyParser.TryParse(text, out var definition);
        Assert.Equal(expected, ok);
        if (expected)
        {
            Assert.False(string.IsNullOrWhiteSpace(definition.DisplayName));
        }
    }

    [Fact]
    public void TryParse_NormalizesDisplayName()
    {
        Assert.True(HotkeyParser.TryParse("ctrl+pgdn", out var definition));
        Assert.Equal("Ctrl+PgDn", definition.DisplayName);
    }
}
