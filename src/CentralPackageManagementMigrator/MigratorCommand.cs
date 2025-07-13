using System.CommandLine;
using CentralPackageManagementMigrator.Builders;
using Microsoft.Extensions.Logging;

namespace CentralPackageManagementMigrator;

internal class MigratorCommand : RootCommand
{
    private const string CommandDescription = "Migrates a codebase to use NuGet central package management (CPM)";

    private readonly Option<LogLevel> _logLevelOption = new("--verbosity", "-v")
    {
        Description = "Verbosity level of the console logging output.",
        DefaultValueFactory = _ => LogLevel.Information
    };

    public MigratorCommand() : base(CommandDescription)
    {
        Options.Add(_logLevelOption);

        SetAction(parseResult =>
        {
            var logLevel = parseResult.GetRequiredValue(_logLevelOption);
            return Migrate(logLevel);
        });
    }

    private static int Migrate(LogLevel logLevel)
    {
        var exitCode = 0;
        LoggingUtility.SetupLogging(logLevel);
        var logger = LoggingUtility.CreateLogger<MigratorCommand>();

        logger.LogDebug("Called with verbosity: {Level}", logLevel.ToString());

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
                var distinctPackages = packages.ToDistinctOrder();

                directoryPackagesProps.WriteFile(distinctPackages);
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
