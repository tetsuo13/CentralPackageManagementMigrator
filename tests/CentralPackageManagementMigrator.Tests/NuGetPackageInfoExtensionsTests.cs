using System.Collections.Generic;
using Xunit;

namespace CentralPackageManagementMigrator.Tests;

public class NuGetPackageInfoExtensionsTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("'$(TargetFramework)' == 'net6.0'", "net6.0")]
    [InlineData("'$(TargetFramework)' == 'net8.0'", "net8.0")]
    public void ExtractTargetFramework_ReturnsExpected(string? condition, string? expected)
    {
        var result = NuGetPackageInfoExtensions.ExtractTargetFramework(condition);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void PromoteConditional_UnconditionalExistsWithSameVersionConditionals_DropsConditionals()
    {
        var packages = new List<NuGetPackageInfo>
        {
            new("Pkg", "1.0.0", null),
            new("Pkg", "1.0.0", "'cond1'"),
            new("Pkg", "1.0.0", "'cond2'")
        };

        var result = NuGetPackageInfoExtensions.PromoteConditional(packages, []);

        Assert.Single(result);
        Assert.Equal("1.0.0", result[0].Version);
        Assert.Null(result[0].Condition);
    }

    [Fact]
    public void PromoteConditional_UnconditionalExistsWithDifferentVersionConditionals_KeepsConditionals()
    {
        var packages = new List<NuGetPackageInfo>
        {
            new("Pkg", "1.0.0", null),
            new("Pkg", "2.0.0", "'$(TargetFramework)' == 'net6.0'"),
            new("Pkg", "3.0.0", "'$(TargetFramework)' == 'net8.0'")
        };

        var result = NuGetPackageInfoExtensions.PromoteConditional(packages, ["net6.0", "net8.0"]);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.Condition == "'$(TargetFramework)' == 'net6.0'" && p.Version == "2.0.0");
        Assert.Contains(result, p => p.Condition == "'$(TargetFramework)' == 'net8.0'" && p.Version == "3.0.0");
        Assert.DoesNotContain(result, p => p.Condition is null);
    }

    [Fact]
    public void PromoteConditional_ConditionalsCoverAllTfsWithDifferentVersions_KeepsConditionals()
    {
        var packages = new List<NuGetPackageInfo>
        {
            new("Pkg", "1.0.0", "'$(TargetFramework)' == 'net6.0'"),
            new("Pkg", "2.0.0", "'$(TargetFramework)' == 'net8.0'")
        };

        var result = NuGetPackageInfoExtensions.PromoteConditional(packages, ["net6.0", "net8.0"]);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.Condition == "'$(TargetFramework)' == 'net6.0'" && p.Version == "1.0.0");
        Assert.Contains(result, p => p.Condition == "'$(TargetFramework)' == 'net8.0'" && p.Version == "2.0.0");
        Assert.DoesNotContain(result, p => p.Condition is null);
    }

    [Fact]
    public void PromoteConditional_ConditionalsCoverAllTfsWithSameVersion_PromotesToUnconditional()
    {
        var packages = new List<NuGetPackageInfo>
        {
            new("Pkg", "1.0.0", "'$(TargetFramework)' == 'net6.0'"),
            new("Pkg", "1.0.0", "'$(TargetFramework)' == 'net8.0'")
        };

        var result = NuGetPackageInfoExtensions.PromoteConditional(packages, ["net6.0", "net8.0"]);

        Assert.Single(result);
        Assert.Equal("1.0.0", result[0].Version);
        Assert.Null(result[0].Condition);
    }

    [Fact]
    public void PromoteConditional_ConditionalsCoverSubset_KeepsPerCondition()
    {
        var packages = new List<NuGetPackageInfo>
        {
            new("Pkg", "1.0.0", "'$(TargetFramework)' == 'net6.0'")
        };

        var result = NuGetPackageInfoExtensions.PromoteConditional(packages, ["net6.0", "net8.0"]);

        Assert.Single(result);
        Assert.Equal("1.0.0", result[0].Version);
        Assert.Equal("'$(TargetFramework)' == 'net6.0'", result[0].Condition);
    }
}
