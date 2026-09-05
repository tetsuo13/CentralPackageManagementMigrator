using System.Reflection;

namespace CentralPackageManagementMigrator.IntegrationTests;

internal sealed class Helper : IDisposable
{
    /// <summary>
    /// Contains absolute paths to various directories used for the test.
    /// </summary>
    /// <param name="Base">
    /// The temporary directory created for the requested test. Will serve as
    /// the working directory for the migrator command to run in.
    /// </param>
    /// <param name="Actual">
    /// Path to the <c>Actual</c> directory that contains setup files for the
    /// test.
    /// </param>
    /// <param name="Expected">
    /// Path to the <c>Expected</c> directory that contains expected files for
    /// the test.
    /// </param>
    private readonly record struct WorkDirectory(string Base, string Actual, string Expected);

    private WorkDirectory WorkDirectoryInfo { get; init; }

    private bool _disposed;

    private Helper()
    {
    }

    ~Helper()
    {
        Dispose(false);
    }

    public static Helper Create(string test)
    {
        var helper = new Helper
        {
            WorkDirectoryInfo = FindWorkDirectory(test)
        };

        // Copy actual files to the work directory.
        CopyDirectory(helper.WorkDirectoryInfo.Actual, helper.WorkDirectoryInfo.Base);

        return helper;
    }

    public async Task<int> RunMigrator()
    {
        Directory.SetCurrentDirectory(WorkDirectoryInfo.Base);

        var command = new MigratorCommand();
        return await command.Parse([]).InvokeAsync(null, TestContext.Current.CancellationToken);
    }

    public async Task AssertDirectoryPackagesFile()
    {
        var directoryPackagesPath = Path.Combine(WorkDirectoryInfo.Base, "Directory.Packages.props");

        Assert.True(Path.Exists(directoryPackagesPath), $"'{directoryPackagesPath}' not found");

        var actual = await File.ReadAllTextAsync(directoryPackagesPath, TestContext.Current.CancellationToken);
        var expected = await File.ReadAllTextAsync(Path.Combine(WorkDirectoryInfo.Expected, "Directory.Packages.xml"),
            TestContext.Current.CancellationToken);

        Assert.Equal(actual, expected);
    }

    public async Task AssertProjectFile(string filename)
    {
        var actualFilename = Path.Combine(WorkDirectoryInfo.Base, filename + ".csproj");
        Assert.True(Path.Exists(actualFilename), $"'{actualFilename}' not found");

        var actual = await File.ReadAllTextAsync(actualFilename, TestContext.Current.CancellationToken);

        var expectedFilename = Path.Combine(WorkDirectoryInfo.Expected, filename + ".xml");
        Assert.True(Path.Exists(expectedFilename), $"'{expectedFilename}' not found");

        var expected = await File.ReadAllTextAsync(expectedFilename, TestContext.Current.CancellationToken);

        Assert.Equal(actual, expected);
    }

    private static WorkDirectory FindWorkDirectory(string test)
    {
        var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        Assert.NotNull(assemblyDirectory);

        // Ensure test data is set up correctly.

        var testDataDirectory = Path.Combine(assemblyDirectory, "TestData", test);
        Assert.True(Directory.Exists(testDataDirectory), $"'{testDataDirectory}' directory cannot be found");

        var actualDirectory = Path.Combine(testDataDirectory, "Actual");
        Assert.True(Directory.Exists(actualDirectory), "'Actual' subdirectory cannot be found");

        var expectedDirectory = Path.Combine(testDataDirectory, "Expected");
        Assert.True(Directory.Exists(expectedDirectory), "'Expected' subdirectory cannot be found");

        var tempDirectory = Directory.CreateTempSubdirectory().FullName;

        return new WorkDirectory(tempDirectory, actualDirectory, expectedDirectory);
    }

    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories"/>
    private static void CopyDirectory(string source, string destination)
    {
        var dir = new DirectoryInfo(source);
        Assert.True(dir.Exists, $"Source directory '{source}' not found");

        var dirs = dir.GetDirectories();

        Directory.CreateDirectory(destination);

        foreach (var file in dir.GetFiles())
        {
            string filename;

            if (file.Name.StartsWith("project", StringComparison.InvariantCultureIgnoreCase))
            {
                filename = Path.GetFileNameWithoutExtension(file.Name) + ".csproj";
            }
            else if (file.Name.StartsWith("directory.packages", StringComparison.InvariantCultureIgnoreCase))
            {
                filename = Path.GetFileNameWithoutExtension(file.Name) + ".props";
            }
            else
            {
                filename = file.Name;
            }

            var target = Path.Combine(destination, filename);
            file.CopyTo(target);
        }

        foreach (var subDirectory in dirs)
        {
            var newDestination = Path.Combine(destination, subDirectory.Name);
            CopyDirectory(subDirectory.FullName, newDestination);
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed || !disposing)
        {
            return;
        }

        try
        {
            Directory.Delete(WorkDirectoryInfo.Base, true);
        }
        catch
        {
            // Let OS prune it eventually
        }

        _disposed = true;
    }
}
