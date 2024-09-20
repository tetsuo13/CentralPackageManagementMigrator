using System.Collections.Generic;
using System.Collections.ObjectModel;
using CentralPackageManagementMigrator.Builders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CentralPackageManagementMigrator.Tests.Builders;

public class ProjectBuilderTests
{
    [Fact]
    public void GetPackagesProjectSource_NoPackageReferences()
    {
        const string csproj = """
                              <Project Sdk="Microsoft.NET.Sdk">

                              </Project>
                              """;

        var packages = GetPackagesProjectSource(csproj);

        Assert.Empty(packages);
    }

    [Theory]
    [InlineData("17.9.0")] // Specific version
    [InlineData("3.6.*")] // Floating version
    [InlineData("3.6.0-beta.*")] // Floating version
    [InlineData("6.*")] // Accepts any 6.x.y version
    [InlineData("(4.1.3,)")] // Accepts any version above, but not including 4.1.3
    [InlineData("(,5.0)")] // Accepts any version up below 5.x
    [InlineData("[1,3)")] // Accepts any 1.x or 2.x version, but not 0.x or 3.x and higher
    [InlineData("[1.3.2,1.5)")] // Accepts 1.3.2 up to 1.4.x, but not 1.5 and higher
    public void GetPackagesProjectSource_PackageReferenceVersions(string version)
    {
        var csproj = $"""
                      <Project Sdk="Microsoft.NET.Sdk">

                        <ItemGroup>
                          <PackageReference Include="Contoso.Utility.UsefulStuff" Version="{version}" />
                        </ItemGroup>

                      </Project>
                      """;

        var packages = GetPackagesProjectSource(csproj);

        Assert.Single(packages);
        Assert.Equal("Contoso.Utility.UsefulStuff", packages[0].Id);
        Assert.Equal(version, packages[0].Version);
    }

    [Fact]
    public void UpdateProjectFromSource_RemoveVersionAttribute()
    {
        var packages = new List<NuGetPackageInfo>
        {
            new("Contoso.Utility.UsefulStuff", "17.9.0")
        };
        const string csproj = $"""
                               <Project Sdk="Microsoft.NET.Sdk">

                                 <ItemGroup>
                                   <PackageReference Include="Contoso.Utility.UsefulStuff" Version="17.9.0" />
                                 </ItemGroup>

                               </Project>
                               """;

        const string expected = $"""
                                 <Project Sdk="Microsoft.NET.Sdk">

                                   <ItemGroup>
                                     <PackageReference Include="Contoso.Utility.UsefulStuff" />
                                   </ItemGroup>

                                 </Project>
                                 """;

        var projectBuilder = GetBuilder();
        var actual = projectBuilder.UpdateProjectFromSource(csproj, packages.AsReadOnly());

        Assert.Equal(expected, actual);
    }

    private static ReadOnlyCollection<NuGetPackageInfo> GetPackagesProjectSource(string csproj)
    {
        var projectBuilder = GetBuilder();
        return projectBuilder.GetPackagesProjectSource(csproj);
    }

    private static ProjectBuilder GetBuilder()
    {
        var logger = NullLoggerFactory.Instance.CreateLogger<ProjectBuilderTests>();
        return new ProjectBuilder(logger);
    }
}
