using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CentralPackageManagementMigrator.Builders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CentralPackageManagementMigrator.Tests;

public class EndToEndTests
{
    [Fact]
    public void FullScenario_CorrectPropsFileAndVersionRemoval()
    {
        var logger = NullLoggerFactory.Instance.CreateLogger<ProjectBuilder>();

        var packages = new ReadOnlyDictionary<string, List<NuGetPackageInfo>>(
            new Dictionary<string, List<NuGetPackageInfo>>
            {
                {
                    "ProjectA",
                    [
                        new NuGetPackageInfo("Newtonsoft.Json", "13.0.3", null)
                    ]
                },
                {
                    "ProjectB",
                    [
                        new NuGetPackageInfo("Newtonsoft.Json", "13.0.3", null),
                        new NuGetPackageInfo("Microsoft.Extensions.Hosting", "6.0.1", "'$(TargetFramework)' == 'net6.0'"),
                        new NuGetPackageInfo("Microsoft.Extensions.Hosting", "8.0.0", "'$(TargetFramework)' == 'net8.0'")
                    ]
                },
                {
                    "ProjectC",
                    [
                        new NuGetPackageInfo("Newtonsoft.Json", "12.0.3", null)
                    ]
                }
            });

        var allTfs = new HashSet<string> { "net6.0", "net8.0" };
        var distinctPackages = packages.ToDistinctOrder(allTfs).ToList();

        Assert.Contains(distinctPackages, p =>
            p.Id == "Newtonsoft.Json" &&
            p.Version == "12.0.3" &&
            p.Condition is null);

        var hostingEntry = Assert.Single(distinctPackages, p => p.Id == "Microsoft.Extensions.Hosting");
        Assert.Equal("6.0.1", hostingEntry.Version);
        Assert.Null(hostingEntry.Condition);

        var propsBuilderLogger = NullLoggerFactory.Instance.CreateLogger<PackagesPropsBuilder>();
        var propsBuilder = new PackagesPropsBuilder(propsBuilderLogger, nameof(FullScenario_CorrectPropsFileAndVersionRemoval));
        var propsXml = propsBuilder.GenerateXml(distinctPackages);

        Assert.Contains("<PackageVersion Include=\"Newtonsoft.Json\" Version=\"12.0.3\" />", propsXml);
        Assert.Contains("<PackageVersion Include=\"Microsoft.Extensions.Hosting\" Version=\"6.0.1\" />", propsXml);

        Assert.DoesNotContain("<PackageVersion Include=\"Microsoft.Extensions.Hosting\" Version=\"8.0.0\" />", propsXml);

        var propertyGroupPos = propsXml.IndexOf("PropertyGroup", System.StringComparison.Ordinal);
        var unconditionalGroupPos = propsXml.IndexOf("<ItemGroup>", System.StringComparison.Ordinal);
        Assert.True(propertyGroupPos < unconditionalGroupPos,
            "PropertyGroup should appear before ItemGroup");

        var hostingPos = propsXml.IndexOf("Microsoft.Extensions.Hosting", System.StringComparison.Ordinal);
        var newtonsoftPos = propsXml.IndexOf("Newtonsoft.Json", System.StringComparison.Ordinal);
        Assert.True(hostingPos < newtonsoftPos,
            "Packages should be sorted alphabetically");
    }

    [Fact]
    public void PartialTfCoverage_PackagesStayConditional()
    {
        var packages = new ReadOnlyDictionary<string, List<NuGetPackageInfo>>(
            new Dictionary<string, List<NuGetPackageInfo>>
            {
                {
                    "ProjectA",
                    [
                        new NuGetPackageInfo("PackageU", "1.0.0", null)
                    ]
                },
                {
                    "ProjectB",
                    [
                        new NuGetPackageInfo("PackageV", "6.0.1", "'$(TargetFramework)' == 'net6.0'"),
                        new NuGetPackageInfo("PackageV", "8.0.0", "'$(TargetFramework)' == 'net8.0'"),
                        new NuGetPackageInfo("PackageW", "6.0.0", "'$(TargetFramework)' == 'net6.0'")
                    ]
                },
                {
                    "ProjectC",
                    [
                        new NuGetPackageInfo("PackageW", "5.0.0", "'$(TargetFramework)' == 'net5.0'")
                    ]
                }
            });

        var allTfs = new HashSet<string> { "net5.0", "net6.0", "net8.0" };
        var distinctPackages = packages.ToDistinctOrder(allTfs).ToList();

        Assert.Contains(distinctPackages, p =>
            p.Id == "PackageU" && p.Version == "1.0.0" && p.Condition is null);

        var packageV = distinctPackages.Where(p => p.Id == "PackageV").ToList();
        Assert.Equal(2, packageV.Count);
        Assert.Contains(packageV, p => p.Condition == "'$(TargetFramework)' == 'net6.0'" && p.Version == "6.0.1");
        Assert.Contains(packageV, p => p.Condition == "'$(TargetFramework)' == 'net8.0'" && p.Version == "8.0.0");

        var packageW = distinctPackages.Where(p => p.Id == "PackageW").ToList();
        Assert.Equal(2, packageW.Count);
        Assert.Contains(packageW, p => p.Condition == "'$(TargetFramework)' == 'net5.0'" && p.Version == "5.0.0");
        Assert.Contains(packageW, p => p.Condition == "'$(TargetFramework)' == 'net6.0'" && p.Version == "6.0.0");

        var propsLogger = NullLoggerFactory.Instance.CreateLogger<PackagesPropsBuilder>();
        var propsBuilder = new PackagesPropsBuilder(propsLogger, nameof(PartialTfCoverage_PackagesStayConditional));
        var propsXml = propsBuilder.GenerateXml(distinctPackages);

        Assert.Contains("<PackageVersion Include=\"PackageV\" Version=\"6.0.1\" />", propsXml);
        Assert.Contains("<PackageVersion Include=\"PackageV\" Version=\"8.0.0\" />", propsXml);
        Assert.Contains("<PackageVersion Include=\"PackageW\" Version=\"5.0.0\" />", propsXml);
        Assert.Contains("<PackageVersion Include=\"PackageW\" Version=\"6.0.0\" />", propsXml);

        var unconditionalPos = propsXml.IndexOf("<ItemGroup>", System.StringComparison.Ordinal);

        var net5Group = propsXml.IndexOf("<ItemGroup Condition=\"'$(TargetFramework)' == 'net5.0'\"", System.StringComparison.Ordinal);
        var net6Group = propsXml.IndexOf("<ItemGroup Condition=\"'$(TargetFramework)' == 'net6.0'\"", System.StringComparison.Ordinal);
        var net8Group = propsXml.IndexOf("<ItemGroup Condition=\"'$(TargetFramework)' == 'net8.0'\"", System.StringComparison.Ordinal);

        Assert.True(unconditionalPos < net5Group, "Unconditional group should come first");
        Assert.True(unconditionalPos < net6Group, "Unconditional group should come first");
        Assert.True(unconditionalPos < net8Group, "Unconditional group should come first");
    }

    [Fact]
    public void MixedPromotion_SomePackagesPromotedSomeStayConditional()
    {
        var packages = new ReadOnlyDictionary<string, List<NuGetPackageInfo>>(
            new Dictionary<string, List<NuGetPackageInfo>>
            {
                {
                    "ProjectA",
                    [
                        new NuGetPackageInfo("PackageX", "6.0.0", "'$(TargetFramework)' == 'net6.0'"),
                        new NuGetPackageInfo("PackageX", "7.0.0", "'$(TargetFramework)' == 'net7.0'"),
                        new NuGetPackageInfo("PackageX", "8.0.0", "'$(TargetFramework)' == 'net8.0'"),
                        new NuGetPackageInfo("PackageY", "6.0.0", "'$(TargetFramework)' == 'net6.0'"),
                        new NuGetPackageInfo("PackageY", "7.0.0", "'$(TargetFramework)' == 'net7.0'")
                    ]
                }
            });

        var allTfs = new HashSet<string> { "net6.0", "net7.0", "net8.0" };
        var distinctPackages = packages.ToDistinctOrder(allTfs).ToList();

        var packageX = Assert.Single(distinctPackages, p => p.Id == "PackageX");
        Assert.Equal("6.0.0", packageX.Version);
        Assert.Null(packageX.Condition);

        var packageY = distinctPackages.Where(p => p.Id == "PackageY").ToList();
        Assert.Equal(2, packageY.Count);
        Assert.Contains(packageY, p => p.Condition == "'$(TargetFramework)' == 'net6.0'" && p.Version == "6.0.0");
        Assert.Contains(packageY, p => p.Condition == "'$(TargetFramework)' == 'net7.0'" && p.Version == "7.0.0");

        var propsLogger = NullLoggerFactory.Instance.CreateLogger<PackagesPropsBuilder>();
        var propsBuilder = new PackagesPropsBuilder(propsLogger, nameof(MixedPromotion_SomePackagesPromotedSomeStayConditional));
        var propsXml = propsBuilder.GenerateXml(distinctPackages);

        Assert.Contains("<PackageVersion Include=\"PackageX\" Version=\"6.0.0\" />", propsXml);
        Assert.Contains("<PackageVersion Include=\"PackageY\" Version=\"6.0.0\" />", propsXml);
        Assert.Contains("<PackageVersion Include=\"PackageY\" Version=\"7.0.0\" />", propsXml);

        var unconditionalPos = propsXml.IndexOf("<ItemGroup>", System.StringComparison.Ordinal);
        var net6GroupPos = propsXml.IndexOf("<ItemGroup Condition=\"'$(TargetFramework)' == 'net6.0'\"", System.StringComparison.Ordinal);
        var net7GroupPos = propsXml.IndexOf("<ItemGroup Condition=\"'$(TargetFramework)' == 'net7.0'\"", System.StringComparison.Ordinal);

        Assert.True(unconditionalPos < net6GroupPos, "Unconditional group should come before conditional groups");
        Assert.True(net6GroupPos < net7GroupPos, "net6.0 group should come before net7.0 group");

        var packageXPos = propsXml.IndexOf("PackageX", System.StringComparison.Ordinal);
        var packageYPos = propsXml.IndexOf("PackageY", System.StringComparison.Ordinal);
        Assert.True(packageXPos < packageYPos, "PackageX should be sorted before PackageY alphabetically");
    }
}
