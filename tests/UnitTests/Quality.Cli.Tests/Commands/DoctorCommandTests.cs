namespace Quality.Cli.Tests.Commands;

using Quality.Cli.Commands;
using Xunit;

public class DoctorCommandTests
{
    [Fact]
    public void Missing_optional_probe_does_not_flip_exit_code()
    {
        // finding #15: docker only backs the docker-quality CI job. A local-only dev whose
        // native toolchain is healthy but who lacks Docker must still get a green doctor.
        var outcomes = new[]
        {
            new DoctorCommand.ProbeOutcome(Required: true, Ok: true),
            new DoctorCommand.ProbeOutcome(Required: true, Ok: true),
            new DoctorCommand.ProbeOutcome(Required: false, Ok: false), // docker missing
        };

        Assert.Equal(0, DoctorCommand.ExitCode(outcomes));
    }

    [Fact]
    public void Missing_required_probe_fails()
    {
        var outcomes = new[]
        {
            new DoctorCommand.ProbeOutcome(Required: true, Ok: false), // e.g. pre-commit missing
            new DoctorCommand.ProbeOutcome(Required: false, Ok: false),
        };

        Assert.Equal(1, DoctorCommand.ExitCode(outcomes));
    }

    [Fact]
    public void All_required_present_passes()
    {
        var outcomes = new[]
        {
            new DoctorCommand.ProbeOutcome(Required: true, Ok: true),
            new DoctorCommand.ProbeOutcome(Required: false, Ok: true),
        };

        Assert.Equal(0, DoctorCommand.ExitCode(outcomes));
    }
}
