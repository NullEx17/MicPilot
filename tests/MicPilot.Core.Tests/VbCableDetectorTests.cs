using MicPilot.Audio;

namespace MicPilot.Core.Tests;

public class VbCableDetectorTests
{
    [Theory]
    [InlineData("CABLE Input (VB-Audio Virtual Cable)", true)]
    [InlineData("CABLE Output (VB-Audio Virtual Cable)", false)]
    [InlineData("Microphone (HyperX Cloud III)", false)]
    public void IsCableInput_MatchesExpected(string name, bool expected) =>
        Assert.Equal(expected, VbCableDetector.IsCableInput(name));

    [Theory]
    [InlineData("CABLE Output (VB-Audio Virtual Cable)", true)]
    [InlineData("CABLE Input (VB-Audio Virtual Cable)", false)]
    public void IsCableOutput_MatchesExpected(string name, bool expected) =>
        Assert.Equal(expected, VbCableDetector.IsCableOutput(name));
}
