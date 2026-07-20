using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace CentralPackageManagementMigrator.Tests.IntegrationTests;

[Trait("Category", "Integration")]
public class EndToEndBuildTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _toolProjectPath;
    private readonly ITestOutputHelper _output;

    public EndToEndBuildTests(ITestOutputHelper output)
    {
        _output = output;
        _testDir = Path.Combine(Path.GetTempPath(), "CPM_E2E_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);

        var assemblyDir = Path.GetDirectoryName(typeof(EndToEndBuildTests).Assembly.Location)!;
        _toolProjectPath = Path.GetFullPath(
            Path.Combine(assemblyDir, "..", "..", "..", "..", "..",
                "src", "CentralPackageManagementMigrator", "CentralPackageManagementMigrator.csproj"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            try { Directory.Delete(_testDir, true); }
            catch { /* best effort cleanup */ }
        }
    }

    [Fact]
    public void SingleProject_SingleTf_BuildSucceeds()
    {
        var projDir = CreateProject("ProjA", "net10.0", ("Newtonsoft.Json", "13.0.3"));

        AssertToolSucceeds();
        AssertPropsContains("Newtonsoft.Json", "13.0.3");
        AssertVersionRemoved(projDir, "Newtonsoft.Json");
        AssertRestoreAndBuild(projDir);
    }

    [Fact]
    public void TwoProjects_SameTf_BuildSucceeds()
    {
        var projA = CreateProject("ProjA", "net10.0", ("Newtonsoft.Json", "13.0.3"));
        var projB = CreateProject("ProjB", "net10.0", ("Microsoft.Extensions.Logging.Abstractions", "10.0.0"));

        AssertToolSucceeds();
        AssertPropsContains("Newtonsoft.Json", "13.0.3");
        AssertPropsContains("Microsoft.Extensions.Logging.Abstractions", "10.0.0");
        AssertVersionRemoved(projA, "Newtonsoft.Json");
        AssertVersionRemoved(projB, "Microsoft.Extensions.Logging.Abstractions");
        AssertRestoreAndBuild(projA);
        AssertRestoreAndBuild(projB);
    }

    [Fact]
    public void SingleProject_MultiTfUnconditional_PropsGenerated()
    {
        var projDir = CreateProject("ProjA", "net10.0;net9.0", ("Newtonsoft.Json", "13.0.3"));

        AssertToolSucceeds();
        AssertPropsContains("Newtonsoft.Json", "13.0.3");
        AssertVersionRemoved(projDir, "Newtonsoft.Json");
    }

    [Fact]
    public void MixedSingleAndMultiTf_BuildSucceeds()
    {
        var projA = CreateProject("ProjA", "net10.0", ("Newtonsoft.Json", "13.0.3"));
        CreateProject("ProjB", "net10.0;net9.0", ("Newtonsoft.Json", "13.0.3"));

        AssertToolSucceeds();
        AssertPropsContains("Newtonsoft.Json", "13.0.3");
        AssertVersionRemoved(projA, "Newtonsoft.Json");
        AssertRestoreAndBuild(projA);
    }

    [Fact]
    public void PreExistingConditionalItemGroup_BuildSucceeds()
    {
        var projDir = CreateConditionalProject("ProjA", "net10.0",
            ("Newtonsoft.Json", "13.0.3"),
            condition: "'$(TargetFramework)' == 'net10.0'");

        AssertToolSucceeds();
        AssertPropsContains("Newtonsoft.Json", "13.0.3");
        AssertVersionRemoved(projDir, "Newtonsoft.Json");
        AssertRestoreAndBuild(projDir);
    }

    [Fact]
    public void VersionConflict_DifferentVersions_BuildSucceeds()
    {
        var projA = CreateProject("ProjA", "net10.0", ("Newtonsoft.Json", "12.0.3"));
        var projB = CreateProject("ProjB", "net10.0", ("Newtonsoft.Json", "13.0.3"));

        AssertToolSucceeds();
        AssertPropsContains("Newtonsoft.Json", "13.0.3");
        AssertVersionRemoved(projA, "Newtonsoft.Json");
        AssertVersionRemoved(projB, "Newtonsoft.Json");
        AssertRestoreAndBuild(projA);
        AssertRestoreAndBuild(projB);
    }

    private string CreateProject(string name, string tfm,
        params (string package, string version)[] packages)
    {
        var projDir = Path.Combine(_testDir, name);
        Directory.CreateDirectory(projDir);
        File.WriteAllText(Path.Combine(projDir, $"{name}.csproj"), BuildCsproj(tfm, packages));
        File.WriteAllText(Path.Combine(projDir, "Program.cs"), BuildProgram(packages));
        return projDir;
    }

    private string CreateConditionalProject(string name, string tfm,
        (string package, string version) package,
        string condition)
    {
        var projDir = Path.Combine(_testDir, name);
        Directory.CreateDirectory(projDir);
        File.WriteAllText(Path.Combine(projDir, $"{name}.csproj"),
            BuildCsproj(tfm, [package], itemGroupCondition: condition));
        File.WriteAllText(Path.Combine(projDir, "Program.cs"), BuildProgram([package]));
        return projDir;
    }

    private static string BuildCsproj(string tfm,
        (string package, string version)[] packages,
        string? itemGroupCondition = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<Project Sdk=\"Microsoft.NET.Sdk\">");
        sb.AppendLine("  <PropertyGroup>");
        sb.AppendLine("    <ImplicitUsings>enable</ImplicitUsings>");
        sb.AppendLine(tfm.Contains(';')
            ? $"    <TargetFrameworks>{tfm}</TargetFrameworks>"
            : $"    <TargetFramework>{tfm}</TargetFramework>");
        sb.AppendLine("    <OutputType>Exe</OutputType>");
        sb.AppendLine("  </PropertyGroup>");

        if (itemGroupCondition is not null)
        {
            sb.AppendLine($"  <ItemGroup Condition=\"{itemGroupCondition}\">");
        }
        else
        {
            sb.AppendLine("  <ItemGroup>");
        }

        foreach (var (package, version) in packages)
        {
            sb.AppendLine($"    <PackageReference Include=\"{package}\" Version=\"{version}\" />");
        }

        sb.AppendLine("  </ItemGroup>");
        sb.AppendLine("</Project>");
        return sb.ToString();
    }

    private static string BuildProgram((string package, string version)[] packages)
    {
        if (packages.Any(p => p.package == "Newtonsoft.Json"))
        {
            return """
                using Newtonsoft.Json;
                var data = new { Value = 42 };
                var json = JsonConvert.SerializeObject(data);
                Console.WriteLine(json);
                """;
        }

        return "Console.WriteLine(\"Hello, World!\");";
    }

    private void AssertToolSucceeds()
    {
        var exitCode = RunTool();
        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(Path.Combine(_testDir, "Directory.Packages.props")));
    }

    private int RunTool()
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{_toolProjectPath}\" -- -p \"{_testDir}\" --verbosity Error",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var process = new Process { StartInfo = psi };
        _output.WriteLine($"Running tool on {_testDir}");
        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit((int)TimeSpan.FromMinutes(3).TotalMilliseconds))
        {
            process.Kill();
            throw new TimeoutException("Tool process timed out after 3 minutes.");
        }

        var stdout = stdoutTask.Result;
        var stderr = stderrTask.Result;

        if (!string.IsNullOrWhiteSpace(stdout))
            _output.WriteLine($"STDOUT: {stdout}");
        if (!string.IsNullOrWhiteSpace(stderr))
            _output.WriteLine($"STDERR: {stderr}");

        return process.ExitCode;
    }

    private int RunDotnet(string command, string workingDir)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = command,
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit((int)TimeSpan.FromMinutes(2).TotalMilliseconds))
        {
            process.Kill();
            throw new TimeoutException("dotnet process timed out.");
        }

        var stdout = stdoutTask.Result;
        var stderr = stderrTask.Result;

        if (!string.IsNullOrWhiteSpace(stdout))
            _output.WriteLine($"STDOUT: {stdout}");
        if (!string.IsNullOrWhiteSpace(stderr))
            _output.WriteLine($"STDERR: {stderr}");

        return process.ExitCode;
    }

    private void AssertRestoreAndBuild(string projDir)
    {
        var csproj = Directory.GetFiles(projDir, "*.csproj").Single();

        _output.WriteLine($"Restoring {csproj}...");
        var restoreExitCode = RunDotnet($"restore \"{csproj}\"", projDir);
        Assert.Equal(0, restoreExitCode);

        _output.WriteLine($"Building {csproj}...");
        var buildExitCode = RunDotnet($"build \"{csproj}\" --no-restore", projDir);
        Assert.Equal(0, buildExitCode);
    }

    private void AssertPropsContains(string packageId, string version)
    {
        var props = File.ReadAllText(Path.Combine(_testDir, "Directory.Packages.props"));
        Assert.Contains($"<PackageVersion Include=\"{packageId}\" Version=\"{version}\"", props);
    }

    private static void AssertVersionRemoved(string projDir, string packageId)
    {
        var csproj = Directory.GetFiles(projDir, "*.csproj").Single();
        var content = File.ReadAllText(csproj);
        Assert.Contains($"<PackageReference Include=\"{packageId}\"", content);
        Assert.DoesNotContain($"<PackageReference Include=\"{packageId}\" Version=", content);
    }
}
