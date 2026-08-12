using MicPilot.Core.Models;
using MicPilot.Core.Settings;

namespace MicPilot.Core.Tests;

public class OverlaySettingsTests
{
    [Fact]
    public void CreateDefault_HasSensibleOverlayDefaults()
    {
        var settings = SettingsStore.CreateDefault();

        Assert.True(settings.OverlayEnabled);
        Assert.True(settings.OverlayShowOnChange);
        Assert.False(settings.OverlayAlwaysVisible);
        Assert.Equal(OverlayPosition.TopRight, settings.OverlayPosition);
        Assert.Equal(2.0, settings.OverlayDurationSeconds);
        Assert.False(settings.HasCompletedFirstRun);
        Assert.False(settings.NotifyOnMuteToggle);
    }
}
