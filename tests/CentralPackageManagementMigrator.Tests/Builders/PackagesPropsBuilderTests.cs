using System.Collections.Generic;
using System.Collections.ObjectModel;
using CentralPackageManagementMigrator.Builders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CentralPackageManagementMigrator.Tests.Builders;

public class PackagesPropsBuilderTests
{
    [Fact]
    public void DistinctPackageIds()
    {
        var packages = new Dictionary<string, ReadOnlyCollection<NuGetPackageInfo>>
        {
            {
                nameof(DistinctPackageIds),
                new ReadOnlyCollection<NuGetPackageInfo>([
                    new NuGetPackageInfo("Contoso.Utility.UsefulStuff", "17.9.0")
                ])
            }
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
    public void PackageIdsInAlphabeticalOrder_SingleProject()
    {
        var packages = new Dictionary<string, ReadOnlyCollection<NuGetPackageInfo>>
        {
            {
                nameof(PackageIdsInAlphabeticalOrder_SingleProject),
                new ReadOnlyCollection<NuGetPackageInfo>([
                    new NuGetPackageInfo("newtonsoft.json", "8.0.3"),
                    new NuGetPackageInfo("jQuery", "3.1.1"),
                    new NuGetPackageInfo("Some.Package", "1.0.0"),
                    new NuGetPackageInfo("NuGet.Core", "2.11.1"),
                    new NuGetPackageInfo("RouteMagic", "1.3"),
                    new NuGetPackageInfo("Contoso.Utility.UsefulStuff", "17.9.0"),
                    new NuGetPackageInfo("Microsoft.Web.Xdt", "2.1.1")
                ])
            }
        };

        const string expected = """
                                <Project>
                                  <PropertyGroup>
                                    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
                                  </PropertyGroup>
                                  <ItemGroup>
                                    <PackageVersion Include="Contoso.Utility.UsefulStuff" Version="17.9.0" />
                                    <PackageVersion Include="jQuery" Version="3.1.1" />
                                    <PackageVersion Include="Microsoft.Web.Xdt" Version="2.1.1" />
                                    <PackageVersion Include="newtonsoft.json" Version="8.0.3" />
                                    <PackageVersion Include="NuGet.Core" Version="2.11.1" />
                                    <PackageVersion Include="RouteMagic" Version="1.3" />
                                    <PackageVersion Include="Some.Package" Version="1.0.0" />
                                  </ItemGroup>
                                </Project>
                                """;

        var actual = GenerateXml(packages);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void PackageIdsInAlphabeticalOrder_MultipleProjects()
    {
        var packages = new Dictionary<string, ReadOnlyCollection<NuGetPackageInfo>>
        {
            {
                "project1",
                new ReadOnlyCollection<NuGetPackageInfo>([
                    new NuGetPackageInfo("Newtonsoft.Json", "8.0.3"),
                    new NuGetPackageInfo("Some.Package", "1.0.0"),
                ])
            },
            {
                "project2",
                new ReadOnlyCollection<NuGetPackageInfo>([
                    new NuGetPackageInfo("jQuery", "3.1.1"),
                    new NuGetPackageInfo("RouteMagic", "1.3"),
                    new NuGetPackageInfo("Microsoft.Web.Xdt", "2.1.1")
                ])
            },
            {
                "project3",
                new ReadOnlyCollection<NuGetPackageInfo>([
                    new NuGetPackageInfo("NuGet.Core", "2.11.1"),
                    new NuGetPackageInfo("Contoso.Utility.UsefulStuff", "17.9.0"),
                ])
            },
        };

        const string expected = """
                                <Project>
                                  <PropertyGroup>
                                    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
                                  </PropertyGroup>
                                  <ItemGroup>
                                    <PackageVersion Include="Contoso.Utility.UsefulStuff" Version="17.9.0" />
                                    <PackageVersion Include="jQuery" Version="3.1.1" />
                                    <PackageVersion Include="Microsoft.Web.Xdt" Version="2.1.1" />
                                    <PackageVersion Include="Newtonsoft.Json" Version="8.0.3" />
                                    <PackageVersion Include="NuGet.Core" Version="2.11.1" />
                                    <PackageVersion Include="RouteMagic" Version="1.3" />
                                    <PackageVersion Include="Some.Package" Version="1.0.0" />
                                  </ItemGroup>
                                </Project>
                                """;

        var actual = GenerateXml(packages);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MultiplePackageIds_SameVersion_AppearsOnlyOnce()
    {
        var packages = new Dictionary<string, ReadOnlyCollection<NuGetPackageInfo>>
        {
            {
                "package1",
                new ReadOnlyCollection<NuGetPackageInfo>([
                    new NuGetPackageInfo("Contoso.Utility.UsefulStuff", "17.9.0")
                ])
            },
            {
                "package2",
                new ReadOnlyCollection<NuGetPackageInfo>([
                    new NuGetPackageInfo("Contoso.Utility.UsefulStuff", "17.9.0")
                ])
            }
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
    public void MultiplePackageIds_DifferentVersions_MinimumUsed()
    {
        var packages = new Dictionary<string, ReadOnlyCollection<NuGetPackageInfo>>
        {
            {
                "package1",
                new ReadOnlyCollection<NuGetPackageInfo>([
                    new NuGetPackageInfo("Contoso.Utility.UsefulStuff", "17.9.0")
                ])
            },
            {
                "package2",
                new ReadOnlyCollection<NuGetPackageInfo>([
                    new NuGetPackageInfo("Contoso.Utility.UsefulStuff", "16.3.6")
                ])
            },
            {
                "package3",
                new ReadOnlyCollection<NuGetPackageInfo>([
                    new NuGetPackageInfo("Contoso.Utility.UsefulStuff", "18.0.0")
                ])
            }
        };

        const string expected = """
                                <Project>
                                  <PropertyGroup>
                                    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
                                  </PropertyGroup>
                                  <ItemGroup>
                                    <PackageVersion Include="Contoso.Utility.UsefulStuff" Version="16.3.6" />
                                  </ItemGroup>
                                </Project>
                                """;

        var actual = GenerateXml(packages);
        Assert.Equal(expected, actual);
    }

    private static string GenerateXml(Dictionary<string, ReadOnlyCollection<NuGetPackageInfo>> packages)
    {
        var logger = NullLoggerFactory.Instance.CreateLogger<PackagesPropsBuilder>();
        var builder =  new PackagesPropsBuilder(logger, nameof(GenerateXml));
        return builder.GenerateXml(packages.AsReadOnly());
    }
}
