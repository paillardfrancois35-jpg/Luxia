using Dmx.Hosting;

namespace Dmx.Integration.Tests;

/// <summary>GEN-096 : la mise en veille est bloquée pendant l'émission, puis de nouveau autorisée.</summary>
public sealed class SleepInhibitorTests
{
    [Fact]
    [Trait("Exigence", "GEN-096")]
    public void StartThenStop_SetsAndReleasesTheRequest()
    {
        using var inhibitor = new SleepInhibitor();

        inhibitor.Start();
        inhibitor.IsActive.ShouldBe(OperatingSystem.IsWindows());

        inhibitor.Stop();
        inhibitor.IsActive.ShouldBeFalse();
    }
}
