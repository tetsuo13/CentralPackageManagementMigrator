using System.CommandLine;
using System.CommandLine.Help;
using System.Linq;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CentralPackageManagementMigrator.Tests;

public class MigratorCommandTests
{
    [Fact]
    public void Command_Options_ContainsHelpDescriptionVerbosity()
    {
        var command = CreateCommand();

        Assert.Equal(3, command.Options.Count);
        Assert.Single(command.Options.OfType<HelpOption>());
        Assert.Single(command.Options.OfType<VersionOption>());
        Assert.Single(command.Options.OfType<Option<LogLevel>>());
    }

    [Fact]
    public void VerbosityOption_Contains_Aliases()
    {
        var command = CreateCommand();
        var verbosityOptions = GetVerbosityOption(command);

        Assert.Equal("--verbosity", verbosityOptions.Name);
        Assert.Single(verbosityOptions.Aliases);
        Assert.Equal("-v", verbosityOptions.Aliases.Single());
    }

    [Fact]
    public void VerbosityOptions_Contains_Description()
    {
        var command = CreateCommand();
        var verbosityOption = GetVerbosityOption(command);

        Assert.NotNull(verbosityOption.Description);
        Assert.Contains("Verbosity", verbosityOption.Description);
    }

    [Fact]
    public void Parse_UnknownOption_ReportsError()
    {
        var command = CreateCommand();
        var parseResult = command.Parse(["--unknown-option-doesnt-exist", nameof(Parse_UnknownOption_ReportsError)]);

        // The first arg isn't recognized as a valid option so the second
        // isn't recognized as the value for the first arg. Instead, they both
        // should be flagged as invalid.
        Assert.Equal(2, parseResult.Errors.Count);
    }

    [Fact]
    public void Parse_NoArguments_UsesInformationVerbosity()
    {
        var command = CreateCommand();
        var verbosityOption = GetVerbosityOption(command);

        var parseResult = command.Parse([]);

        Assert.Empty(parseResult.Errors);
        Assert.Equal(LogLevel.Information, parseResult.GetValue(verbosityOption));
    }

    [Theory]
    [InlineData("--verbosity", "Debug", LogLevel.Debug)]
    [InlineData("--verbosity", "Warning", LogLevel.Warning)]
    [InlineData("--verbosity", "None", LogLevel.None)]
    [InlineData("-v", "Warning", LogLevel.Warning)]
    public void Parse_Verbosity_ReturnsLogLevel(string flag, string flagValue, LogLevel expectedLevel)
    {
        var command = CreateCommand();
        var verbosityOption = GetVerbosityOption(command);

        var parseResult = command.Parse([flag, flagValue]);

        Assert.Empty(parseResult.Errors);
        Assert.Equal(expectedLevel, parseResult.GetValue(verbosityOption));
    }

    [Fact]
    public void Parse_VerbosityInvalid_ReportsError()
    {
        var command = CreateCommand();
        var parseResult = command.Parse(["--verbosity", nameof(Parse_VerbosityInvalid_ReportsError)]);

        Assert.Single(parseResult.Errors);
    }

    private static MigratorCommand CreateCommand() => [];

    private static Option<LogLevel> GetVerbosityOption(MigratorCommand command) =>
        command.Options.OfType<Option<LogLevel>>().Single();
}
