using System.Collections.Generic;
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
    public void GetPackagesProjectSource_VersionAsChildElement()
    {
        const string version = "1.37.3";
        const string csproj = $"""
                               <Project Sdk="Microsoft.NET.Sdk">

                                 <ItemGroup>
                                   <PackageReference Include="Contoso.Utility.UsefulStuff">
                                     <Version>{version}</Version>
                                   </PackageReference>
                                 </ItemGroup>

                               </Project>
                               """;

        var packages = GetPackagesProjectSource(csproj);

        Assert.Single(packages);
        Assert.Equal("Contoso.Utility.UsefulStuff", packages[0].Id);
        Assert.Equal(version, packages[0].Version);
    }

    [Fact]
    public void GetPackagesProjectSource_VersionAsChildElementWithOtherChildren()
    {
        const string version = "1.37.3";
        const string csproj = $"""
                               <Project Sdk="Microsoft.NET.Sdk">

                                 <ItemGroup>
                                   <PackageReference Include="Contoso.Utility.UsefulStuff">
                                     <Version>{version}</Version>
                                     <ExcludeAssets>compile</ExcludeAssets>
                                   </PackageReference>
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
        const string csproj = """
                              <Project Sdk="Microsoft.NET.Sdk">

                                <ItemGroup>
                                  <PackageReference Include="Contoso.Utility.UsefulStuff" Version="17.9.0" />
                                </ItemGroup>

                              </Project>
                              """;

        const string expected = """
                                <Project Sdk="Microsoft.NET.Sdk">

                                  <ItemGroup>
                                    <PackageReference Include="Contoso.Utility.UsefulStuff" />
                                  </ItemGroup>

                                </Project>
                                """;

        UpdateProjectFromSource(csproj, expected);
    }

    [Fact]
    public void UpdateProjectFromSource_ProjectAssetsRetained()
    {
        const string csproj = """
                              <Project Sdk="Microsoft.NET.Sdk">

                                <ItemGroup>
                                  <PackageReference Include="Contoso.Utility.UsefulStuff" Version="3.6.0">
                                    <ExcludeAssets>compile</ExcludeAssets>
                                    <PrivateAssets>contentFiles</PrivateAssets>
                                  </PackageReference>
                                </ItemGroup>

                              </Project>
                              """;

        const string expected = """
                                <Project Sdk="Microsoft.NET.Sdk">

                                  <ItemGroup>
                                    <PackageReference Include="Contoso.Utility.UsefulStuff">
                                      <ExcludeAssets>compile</ExcludeAssets>
                                      <PrivateAssets>contentFiles</PrivateAssets>
                                    </PackageReference>
                                  </ItemGroup>

                                </Project>
                                """;

        UpdateProjectFromSource(csproj, expected);
    }

    [Fact]
    public void UpdateProjectFromSource_PackageReferenceCondition()
    {
        const string csproj = """
                              <Project Sdk="Microsoft.NET.Sdk">

                                <ItemGroup>
                                  <PackageReference Include="Contoso.Utility.UsefulStuff" Version="3.6.0" Condition="'$(TargetFramework)' == 'net452'" />
                                </ItemGroup>

                              </Project>
                              """;

        const string expected = """
                                <Project Sdk="Microsoft.NET.Sdk">

                                  <ItemGroup>
                                    <PackageReference Include="Contoso.Utility.UsefulStuff" Condition="'$(TargetFramework)' == 'net452'" />
                                  </ItemGroup>

                                </Project>
                                """;

        UpdateProjectFromSource(csproj, expected);
    }

    [Fact]
    public void UpdateProjectFromSource_VersionAsChildElement()
    {
        const string csproj = """
                              <Project Sdk="Microsoft.NET.Sdk">

                                <ItemGroup>
                                  <PackageReference Include="Contoso.Utility.UsefulStuff">
                                    <Version>3.6.0</Version>
                                  </PackageReference>
                                </ItemGroup>

                              </Project>
                              """;

        const string expected = """
                                <Project Sdk="Microsoft.NET.Sdk">

                                  <ItemGroup>
                                    <PackageReference Include="Contoso.Utility.UsefulStuff" />
                                  </ItemGroup>

                                </Project>
                                """;

        UpdateProjectFromSource(csproj, expected);
    }

    [Fact]
    public void UpdateProjectFromSource_VersionAsChildElementWithExcludeAssets()
    {
        const string csproj = """
                              <Project Sdk="Microsoft.NET.Sdk">

                                <ItemGroup>
                                  <PackageReference Include="Contoso.Utility.UsefulStuff">
                                    <Version>3.6.0</Version>
                                    <ExcludeAssets>compile</ExcludeAssets>
                                  </PackageReference>
                                </ItemGroup>

                              </Project>
                              """;

        const string expected = """
                                <Project Sdk="Microsoft.NET.Sdk">

                                  <ItemGroup>
                                    <PackageReference Include="Contoso.Utility.UsefulStuff">
                                      <ExcludeAssets>compile</ExcludeAssets>
                                    </PackageReference>
                                  </ItemGroup>

                                </Project>
                                """;

        UpdateProjectFromSource(csproj, expected);
    }

    [Fact]
    public void GetPackagesProjectSource_PackageReferenceInPlainItemGroup_ConditionIsNull()
    {
        const string csproj = """
                              <Project Sdk="Microsoft.NET.Sdk">
                                <ItemGroup>
                                  <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
                                </ItemGroup>
                              </Project>
                              """;

        var packages = GetPackagesProjectSource(csproj);

        Assert.Single(packages);
        Assert.Equal("Newtonsoft.Json", packages[0].Id);
        Assert.Null(packages[0].Condition);
    }

    [Fact]
    public void GetPackagesProjectSource_PackageReferenceInConditionalItemGroup_ConditionMatches()
    {
        const string csproj = """
                              <Project Sdk="Microsoft.NET.Sdk">
                                <ItemGroup Condition="'$(TargetFramework)' == 'net6.0'">
                                  <PackageReference Include="SomePackage" Version="6.0.1" />
                                </ItemGroup>
                              </Project>
                              """;

        var packages = GetPackagesProjectSource(csproj);

        Assert.Single(packages);
        Assert.Equal("SomePackage", packages[0].Id);
        Assert.Equal("'$(TargetFramework)' == 'net6.0'", packages[0].Condition);
    }

    [Fact]
    public void GetPackagesProjectSource_MultipleConditionalItemGroups_EachCarriesOwnCondition()
    {
        const string csproj = """
                              <Project Sdk="Microsoft.NET.Sdk">
                                <ItemGroup Condition="'$(TargetFramework)' == 'net6.0'">
                                  <PackageReference Include="PackageA" Version="6.0.0" />
                                </ItemGroup>
                                <ItemGroup Condition="'$(TargetFramework)' == 'net8.0'">
                                  <PackageReference Include="PackageB" Version="8.0.0" />
                                </ItemGroup>
                              </Project>
                              """;

        var packages = GetPackagesProjectSource(csproj);

        Assert.Equal(2, packages.Count);
        Assert.Equal("PackageA", packages[0].Id);
        Assert.Equal("'$(TargetFramework)' == 'net6.0'", packages[0].Condition);
        Assert.Equal("PackageB", packages[1].Id);
        Assert.Equal("'$(TargetFramework)' == 'net8.0'", packages[1].Condition);
    }

    [Fact]
    public void GetPackagesProjectSource_PackageReferenceNoVersion_Skipped()
    {
        const string csproj = """
                              <Project Sdk="Microsoft.NET.Sdk">
                                <ItemGroup>
                                  <PackageReference Include="SomePackage" />
                                </ItemGroup>
                              </Project>
                              """;

        var packages = GetPackagesProjectSource(csproj);

        Assert.Empty(packages);
    }

    [Fact]
    public void GetPackagesProjectSource_NoPackageReferences_ReturnsEmpty()
    {
        const string csproj = """
                              <Project Sdk="Microsoft.NET.Sdk">
                              </Project>
                              """;

        var packages = GetPackagesProjectSource(csproj);

        Assert.Empty(packages);
    }

    [Fact]
    public void UpdateProjectFromSource_VersionAttributeInConditionalItemGroup_Removed()
    {
        const string csproj = """
                              <Project Sdk="Microsoft.NET.Sdk">
                                <ItemGroup Condition="'$(TargetFramework)' == 'net6.0'">
                                  <PackageReference Include="Contoso.Utility.UsefulStuff" Version="6.0.1" />
                                </ItemGroup>
                              </Project>
                              """;

        const string expected = """
                                <Project Sdk="Microsoft.NET.Sdk">
                                  <ItemGroup Condition="'$(TargetFramework)' == 'net6.0'">
                                    <PackageReference Include="Contoso.Utility.UsefulStuff" />
                                  </ItemGroup>
                                </Project>
                                """;

        UpdateProjectFromSource(csproj, expected);
    }

    [Fact]
    public void UpdateProjectFromSource_VersionAsChildElementInConditionalItemGroup_Removed()
    {
        const string csproj = """
                              <Project Sdk="Microsoft.NET.Sdk">
                                <ItemGroup Condition="'$(TargetFramework)' == 'net6.0'">
                                  <PackageReference Include="Contoso.Utility.UsefulStuff">
                                    <Version>6.0.1</Version>
                                  </PackageReference>
                                </ItemGroup>
                              </Project>
                              """;

        const string expected = """
                                <Project Sdk="Microsoft.NET.Sdk">
                                  <ItemGroup Condition="'$(TargetFramework)' == 'net6.0'">
                                    <PackageReference Include="Contoso.Utility.UsefulStuff" />
                                  </ItemGroup>
                                </Project>
                                """;

        UpdateProjectFromSource(csproj, expected);
    }

    [Fact]
    public void UpdateProjectFromSource_SamePackageInConditionalAndUnconditional_VersionRemovedFromBoth()
    {
        const string csproj = """
                              <Project Sdk="Microsoft.NET.Sdk">
                                <ItemGroup>
                                  <PackageReference Include="SomePackage" Version="1.0.0" />
                                </ItemGroup>
                                <ItemGroup Condition="'$(TargetFramework)' == 'net8.0'">
                                  <PackageReference Include="SomePackage" Version="8.0.0" />
                                </ItemGroup>
                              </Project>
                              """;

        const string expected = """
                                <Project Sdk="Microsoft.NET.Sdk">
                                  <ItemGroup>
                                    <PackageReference Include="SomePackage" />
                                  </ItemGroup>
                                  <ItemGroup Condition="'$(TargetFramework)' == 'net8.0'">
                                    <PackageReference Include="SomePackage" />
                                  </ItemGroup>
                                </Project>
                                """;

        var packages = new List<NuGetPackageInfo>
        {
            new("SomePackage", "1.0.0", null),
            new("SomePackage", "8.0.0", "'$(TargetFramework)' == 'net8.0'")
        };

        var projectBuilder = GetBuilder();
        var actual = projectBuilder.UpdateProjectFromSource(csproj, packages);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GetPackagesProjectSource_SamePackageInConditionalAndUnconditional_BothCaptured()
    {
        const string csproj = """
                              <Project Sdk="Microsoft.NET.Sdk">
                                <ItemGroup>
                                  <PackageReference Include="SomePackage" Version="1.0.0" />
                                </ItemGroup>
                                <ItemGroup Condition="'$(TargetFramework)' == 'net8.0'">
                                  <PackageReference Include="SomePackage" Version="8.0.0" />
                                </ItemGroup>
                              </Project>
                              """;

        var packages = GetPackagesProjectSource(csproj);

        Assert.Equal(2, packages.Count);
        var unconditional = Assert.Single(packages, p => p.Condition is null);
        Assert.Equal("1.0.0", unconditional.Version);
        var conditional = Assert.Single(packages, p => p.Condition is not null);
        Assert.Equal("'$(TargetFramework)' == 'net8.0'", conditional.Condition);
        Assert.Equal("8.0.0", conditional.Version);
    }

    [Fact]
    public void GetTargetFrameworks_SingleTargetFramework_ReturnsOne()
    {
        var frameworks = GetTargetFrameworksFromSource("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        Assert.Equal(new HashSet<string> { "net8.0" }, frameworks);
    }

    [Fact]
    public void GetTargetFrameworks_MultipleTargetFrameworks_ReturnsAll()
    {
        var frameworks = GetTargetFrameworksFromSource("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFrameworks>net6.0;net8.0</TargetFrameworks>
              </PropertyGroup>
            </Project>
            """);

        Assert.Equal(new HashSet<string> { "net6.0", "net8.0" }, frameworks);
    }

    private static HashSet<string> GetTargetFrameworksFromSource(string csprojSource)
    {
        var projectBuilder = GetBuilder();
        return projectBuilder.GetTargetFrameworksFromSource(csprojSource);
    }

    private static List<NuGetPackageInfo> GetPackagesProjectSource(string csproj)
    {
        var projectBuilder = GetBuilder();
        return projectBuilder.GetPackagesProjectSource(csproj);
    }

    private static ProjectBuilder GetBuilder()
    {
        var logger = NullLoggerFactory.Instance.CreateLogger<ProjectBuilderTests>();
        return new ProjectBuilder(logger);
    }

    private static void UpdateProjectFromSource(string inputProject, string expectedProject)
    {
        var packages = new List<NuGetPackageInfo> { new("Contoso.Utility.UsefulStuff", "3.6.0") };

        var projectBuilder = GetBuilder();
        var actual = projectBuilder.UpdateProjectFromSource(inputProject, packages);

        Assert.Equal(expectedProject, actual);
    }
}
