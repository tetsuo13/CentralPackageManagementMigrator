using System.Linq;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CentralPackageManagementMigrator.Tests;

public class MigratorCommandTests
{
    private const string LogLevelOptionName = "--verbosity";

    [Fact]
    public void Constructor_ShouldInitialize_VerbosityOptionByName()
    {
        var command = new MigratorCommand();
        Assert.Contains(command.Options, option => option.Name == LogLevelOptionName);
    }

    [Fact]
    public void Constructor_ShouldInitialize_VerbosityOptionByAlias()
    {
        var command = new MigratorCommand();

        var logLevelOption = command.Options.Single(option => option.Name == LogLevelOptionName);
        Assert.Single(logLevelOption.Aliases);
        Assert.Equal("-v", logLevelOption.Aliases.First());
    }

    [Fact]
    public void Constructor_ShouldInitialize_DefaultVerbosityOption()
    {
        var command = new MigratorCommand();

        var logLevelOption = command.Options.Single(option => option.Name == LogLevelOptionName);
        Assert.Equal(LogLevel.Information, logLevelOption.GetDefaultValue());
    }
}
