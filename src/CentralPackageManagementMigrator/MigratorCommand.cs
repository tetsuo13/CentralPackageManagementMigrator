using System.CommandLine;
using CentralPackageManagementMigrator.Builders;
using Microsoft.Extensions.Logging;

namespace CentralPackageManagementMigrator;

internal class MigratorCommand : Command
{
    // TODO: Should match NuGet spec
    private const string CommandName = "centralpackagemanagement-migrator";
    private const string CommandDescription = "Migrates a codebase to use NuGet central package management (CPM)";

    private readonly Option<LogLevel> _logLevelOption = new(["-v", "--verbosity"],
        () => LogLevel.Information,
        "Verbosity level of the console logging output.");

    public MigratorCommand() : base(CommandName, CommandDescription)
    {
        AddOption(_logLevelOption);
        this.SetHandler(logLevel => Migrate(logLevel), _logLevelOption);
    }

    private static int Migrate(LogLevel logLevel)
    {
        var exitCode = 0;
        LoggingUtility.SetupLogging(logLevel);
        var logger = LoggingUtility.CreateLogger<MigratorCommand>();

        logger.LogDebug("Called with log level {LogLevel}", logLevel);

        var searchPath = Directory.GetCurrentDirectory();
        logger.LogInformation("Adding central package management under search path: {SearchPath}", searchPath);

        var directoryPackagesProps = new PackagesPropsBuilder(LoggingUtility.CreateLogger<PackagesPropsBuilder>(),
            searchPath);

        if (directoryPackagesProps.Exists)
        {
            logger.LogWarning("{FilePath} file already exists, central package management may already be set up",
                PackagesPropsBuilder.TargetFileName);
            exitCode = 1;
        }
        else
        {
            var projectBuilder = new ProjectBuilder(LoggingUtility.CreateLogger<ProjectBuilder>());
            var packages = projectBuilder.GetPackagesInAllProjects(searchPath);

            if (packages.Count > 0)
            {
                directoryPackagesProps.WriteFile(packages);
                projectBuilder.UpdateProjects(packages);
            }
            else
            {
                logger.LogInformation("No packages were found, nothing more to do");
            }
        }

        logger.LogInformation("Setup of central package management is complete");

        LoggingUtility.FlushLogging();
        return exitCode;
    }
}
