using Backend.Helpers;

namespace Backend.Tests.UnitTests.Helpers;

public class StartupCommandLineTests
{
    [Fact]
    public void ExitAfterStart_false_when_absent()
    {
        Assert.False(StartupCommandLine.ExitAfterStart([]));
        Assert.False(StartupCommandLine.ExitAfterStart(["--skip-database"]));
    }

    [Fact]
    public void ExitAfterStart_true_when_present()
    {
        Assert.True(StartupCommandLine.ExitAfterStart(["--exit-after-start"]));
        Assert.True(StartupCommandLine.ExitAfterStart(["--urls", "http://localhost:5016", "--exit-after-start"]));
    }

    [Fact]
    public void SkipDatabase_true_when_skip_database_flag_present()
    {
        Assert.True(StartupCommandLine.SkipDatabase(["--skip-database"]));
    }

    [Fact]
    public void SkipDatabase_true_when_exit_after_start_is_present()
    {
        Assert.True(StartupCommandLine.SkipDatabase(["--exit-after-start"]));
    }

    [Fact]
    public void SkipDatabase_false_when_neither_flag_present()
    {
        Assert.False(StartupCommandLine.SkipDatabase([]));
        Assert.False(StartupCommandLine.SkipDatabase(["--urls", "http://localhost:5016"]));
    }
}
