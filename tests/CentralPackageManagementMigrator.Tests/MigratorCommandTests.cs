using System.CommandLine;
using System.CommandLine.Help;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CentralPackageManagementMigrator.Tests;

public class MigratorCommandTests
{
    [Fact]
    public void Command_Options_ContainsHelpDescriptionVerbosityAndPath()
    {
        var command = CreateCommand();

        Assert.Equal(4, command.Options.Count);
        Assert.Single(command.Options.OfType<HelpOption>());
        Assert.Single(command.Options.OfType<VersionOption>());
        Assert.Single(command.Options.OfType<Option<LogLevel>>());
        Assert.Single(command.Options.OfType<Option<DirectoryInfo>>());
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
    public void PathOption_Contains_Aliases()
    {
        var command = CreateCommand();
        var pathOption = GetPathOption(command);

        Assert.Equal("--path", pathOption.Name);
        Assert.Single(pathOption.Aliases);
        Assert.Equal("-p", pathOption.Aliases.Single());
    }

    [Fact]
    public void PathOption_Contains_Description()
    {
        var command = CreateCommand();
        var pathOption = GetPathOption(command);

        Assert.NotNull(pathOption.Description);
        Assert.Contains("Directory", pathOption.Description);
    }

    [Fact]
    public void Parse_UnknownOption_ReportsError()
    {
        var command = CreateCommand();
        var parseResult = command.Parse(["--unknown-option-doesnt-exist", nameof(Parse_UnknownOption_ReportsError)]);

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

    [Fact]
    public void Parse_NoArguments_UsesCurrentDirectory()
    {
        var command = CreateCommand();
        var pathOption = GetPathOption(command);

        var parseResult = command.Parse([]);

        Assert.Empty(parseResult.Errors);
        Assert.NotNull(parseResult.GetValue(pathOption));
        Assert.Equal(Directory.GetCurrentDirectory(), parseResult.GetValue(pathOption)!.FullName);
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

    [Theory]
    [InlineData("--path", "/tmp")]
    [InlineData("-p", "/tmp")]
    public void Parse_Path_ReturnsDirectory(string flag, string pathValue)
    {
        var command = CreateCommand();
        var pathOption = GetPathOption(command);

        var parseResult = command.Parse([flag, pathValue]);

        Assert.Empty(parseResult.Errors);
        Assert.NotNull(parseResult.GetValue(pathOption));
        Assert.Equal(pathValue, parseResult.GetValue(pathOption)!.FullName);
    }

    [Fact]
    public void Parse_PathInvalid_ReportsError()
    {
        var command = CreateCommand();
        var parseResult = command.Parse(["--path", ""]);

        Assert.Single(parseResult.Errors);
    }

    private static MigratorCommand CreateCommand() => [];

    private static Option<LogLevel> GetVerbosityOption(MigratorCommand command) =>
        command.Options.OfType<Option<LogLevel>>().Single();

    private static Option<DirectoryInfo> GetPathOption(MigratorCommand command) =>
        command.Options.OfType<Option<DirectoryInfo>>().Single();
}
