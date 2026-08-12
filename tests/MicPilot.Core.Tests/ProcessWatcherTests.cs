using MicPilot.Profiles;

namespace MicPilot.Core.Tests;

public sealed class ProcessWatcherTests
{
    [Theory]
    [InlineData("FiveM_GTAProcess", "FiveM_GTAProcess")]
    [InlineData("FiveM_GTAProcess.exe", "FiveM_GTAProcess.exe")]
    [InlineData("FiveM_b3095_GTAProcess", "FiveM_GTAProcess.exe")]
    [InlineData("FiveM_b2802_GTAProcess", "FiveM_GTAProcess")]
    [InlineData("FiveM_GameProcess", "FiveM_GTAProcess.exe")]
    [InlineData("FiveM_b3407_GameProcess", "FiveM_GTAProcess.exe")]
    [InlineData("FiveM", "FiveM_GTAProcess.exe")]
    [InlineData("FiveM.exe", "FiveM_GTAProcess")]
    public void FiveM_variants_match(string running, string configured)
    {
        Assert.True(ProcessWatcher.MatchesProcessName(running, configured));
    }

    [Theory]
    [InlineData("Discord", "FiveM_GTAProcess.exe")]
    [InlineData("GTA5", "FiveM_GTAProcess.exe")]
    [InlineData("FiveM_DumpServer", "FiveM_GTAProcess.exe")]
    [InlineData("VALORANT-Win64-Shipping", "FiveM_GTAProcess.exe")]
    public void Unrelated_processes_do_not_match_FiveM(string running, string configured)
    {
        Assert.False(ProcessWatcher.MatchesProcessName(running, configured));
    }

    [Fact]
    public void Valorant_exact_match_only()
    {
        Assert.True(ProcessWatcher.MatchesProcessName("VALORANT-Win64-Shipping", "VALORANT-Win64-Shipping.exe"));
        Assert.False(ProcessWatcher.MatchesProcessName("RiotClientServices", "VALORANT-Win64-Shipping.exe"));
    }
}
