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

    private readonly Option<DirectoryInfo> _pathOption = new("--path", "-p")
    {
        Description = "Directory to search for project files under.",
        DefaultValueFactory = _ => new DirectoryInfo(Directory.GetCurrentDirectory())
    };

    public MigratorCommand() : base(CommandDescription)
    {
        Options.Add(_logLevelOption);
        Options.Add(_pathOption);

        SetAction(parseResult =>
        {
            var logLevel = parseResult.GetRequiredValue(_logLevelOption);
            var path = parseResult.GetRequiredValue(_pathOption);
            return Migrate(logLevel, path);
        });
    }

    private static int Migrate(LogLevel logLevel, DirectoryInfo? path = null)
    {
        var exitCode = 0;
        LoggingUtility.SetupLogging(logLevel);
        var logger = LoggingUtility.CreateLogger<MigratorCommand>();

        logger.LogDebug("Called with verbosity: {Level}", logLevel.ToString());

        var searchPath = path?.FullName ?? Directory.GetCurrentDirectory();
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
            var allTargetFrameworks = projectBuilder.GetTargetFrameworks(searchPath);
            var packages = projectBuilder.GetPackagesInAllProjects(searchPath);

            if (packages.Count > 0)
            {
                var distinctPackages = packages.ToDistinctOrder(allTargetFrameworks);

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
