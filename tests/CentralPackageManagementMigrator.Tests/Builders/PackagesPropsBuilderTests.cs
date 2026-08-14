using System.Collections.Generic;
using System.Linq;
using CentralPackageManagementMigrator.Builders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CentralPackageManagementMigrator.Tests.Builders;

public class PackagesPropsBuilderTests
{
    [Fact]
    public void BasicExample()
    {
        var packages = new List<NuGetPackageInfo>
        {
            new("Contoso.Utility.UsefulStuff", "17.9.0")
        };

        const string expected = """
                                <Project>
                                  <PropertyGroup>
                                    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
                                  </PropertyGroup>
                                  <ItemGroup>
                                    <PackageVersion Include="Contoso.Utility.UsefulStuff" Version="17.9.0" />
                                  </ItemGroup>
                                </Project>
                                """;

        var actual = GenerateXml(packages);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MixOfUnconditionalAndConditional_UnconditionalGroupFirst()
    {
        var packages = new List<NuGetPackageInfo>
        {
            new("SomePackage", "8.0.0", "'$(TargetFramework)' == 'net8.0'"),
            new("UniversalPackage", "1.0.0", null)
        };

        const string expected = """
                                <Project>
                                  <PropertyGroup>
                                    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
                                  </PropertyGroup>
                                  <ItemGroup>
                                    <PackageVersion Include="UniversalPackage" Version="1.0.0" />
                                  </ItemGroup>
                                  <ItemGroup Condition="'$(TargetFramework)' == 'net8.0'">
                                    <PackageVersion Include="SomePackage" Version="8.0.0" />
                                  </ItemGroup>
                                </Project>
                                """;

        var actual = GenerateXml(packages);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MultipleConditionalGroups_EachWithCorrectCondition()
    {
        var packages = new List<NuGetPackageInfo>
        {
            new("PackageA", "6.0.0", "'$(TargetFramework)' == 'net6.0'"),
            new("PackageB", "8.0.0", "'$(TargetFramework)' == 'net8.0'")
        };

        const string expected = """
                                <Project>
                                  <PropertyGroup>
                                    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
                                  </PropertyGroup>
                                  <ItemGroup Condition="'$(TargetFramework)' == 'net6.0'">
                                    <PackageVersion Include="PackageA" Version="6.0.0" />
                                  </ItemGroup>
                                  <ItemGroup Condition="'$(TargetFramework)' == 'net8.0'">
                                    <PackageVersion Include="PackageB" Version="8.0.0" />
                                  </ItemGroup>
                                </Project>
                                """;

        var actual = GenerateXml(packages);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void PackagesSortedAlphabeticallyWithinGroups()
    {
        var packages = new List<NuGetPackageInfo>
        {
            new("ZPackage", "1.0.0", null),
            new("APackage", "1.0.0", null)
        };

        var actual = GenerateXml(packages);

        var aPos = actual.IndexOf("APackage");
        var zPos = actual.IndexOf("ZPackage");
        Assert.True(aPos < zPos, "APackage should appear before ZPackage");
    }

    [Fact]
    public void ConditionAttributeCopiedVerbatim()
    {
        var packages = new List<NuGetPackageInfo>
        {
            new("SomePackage", "6.0.0", "'$(TargetFramework)' == 'net6.0'")
        };

        var actual = GenerateXml(packages);

        Assert.Contains("Condition=\"'$(TargetFramework)' == 'net6.0'\"", actual);
    }

    private static string GenerateXml(List<NuGetPackageInfo> packages)
    {
        var logger = NullLoggerFactory.Instance.CreateLogger<PackagesPropsBuilder>();
        var builder =  new PackagesPropsBuilder(logger, nameof(GenerateXml));
        return builder.GenerateXml(packages);
    }
}
