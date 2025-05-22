using System.Collections.Generic;
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

    private static string GenerateXml(List<NuGetPackageInfo> packages)
    {
        var logger = NullLoggerFactory.Instance.CreateLogger<PackagesPropsBuilder>();
        var builder =  new PackagesPropsBuilder(logger, nameof(GenerateXml));
        return builder.GenerateXml(packages);
    }
}
