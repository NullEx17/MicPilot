using MicPilot.Core.Settings;

namespace MicPilot.Core.Tests;

public class SettingsStoreTests
{
    [Fact]
    public void CreateDefault_IncludesFiveMProfile()
    {
        var settings = SettingsStore.CreateDefault();

        Assert.Contains(settings.Profiles, profile => profile.Name == "FiveM");
        Assert.Contains(settings.Profiles, profile => profile.ProcessName == "FiveM_GTAProcess.exe");
    }

    [Fact]
    public void CreateDefault_SetsActiveProfile()
    {
        var settings = SettingsStore.CreateDefault();

        Assert.NotNull(settings.ActiveProfileId);
        Assert.Contains(settings.Profiles, profile => profile.Id == settings.ActiveProfileId);
    }
}
