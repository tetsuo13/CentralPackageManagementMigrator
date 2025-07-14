using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace CentralPackageManagementMigrator.Tests;

public class NuGetPackageInfoTests
{
    [Theory]
    [InlineData("NUnit", "1.2.3", "nunit", "1.2.3", true)]
    [InlineData("NUnit", "1.2.3", "nunit", "4.5.6", false)]
    [InlineData("NUnit", "1.2.3", "NUnit", "1.2.3", true)]
    [InlineData("NUnit", "1.2.3", "NUnit", "4.5.6", false)]
    [InlineData("Contoso.Utility.UsefulStuff", "4.5.6", "jQuery", "3.1.1", false)]
    [InlineData("Contoso.Utility.UsefulStuff", "4.5.6", "jQuery", "4.5.6", false)]
    public void Equality(string leftId, string leftVersion, string rightId, string rightVersion, bool shouldBeEqual)
    {
        var left = new NuGetPackageInfo(leftId, leftVersion);
        var right = new NuGetPackageInfo(rightId, rightVersion);
        Assert.Equal(shouldBeEqual, left.Equals(right));
    }

    [Fact]
    public void EqualityUsingLinqDistinct()
    {
        List<NuGetPackageInfo> packages =
        [
            new("NUnit", "1.2.3"),
            new("Contoso.Utility.UsefulStuff", "4.5.6"),
            new("nunit", "1.2.3")
        ];

        var actual = packages.Distinct().ToList();

        Assert.Equal(2, actual.Count);
        Assert.Same(packages[0], actual[0]);
        Assert.Same(packages[1], actual[1]);
    }

    [Fact]
    public void EqualityUsingEquals_Null_ReturnsFalse()
    {
        var package = new NuGetPackageInfo("NUnit", "1.2.3");
        Assert.False(package.Equals(null));
    }

    [Fact]
    public void EqualityUsingEquals_ReferenceSameObject_ReturnsTrue()
    {
        var package = new NuGetPackageInfo("NUnit", "1.2.3");
        Assert.True(package.Equals(package));
    }

    [Fact]
    public void ToDistinctOrder()
    {
        var packages = new Dictionary<string, List<NuGetPackageInfo>>
        {
            { "ProjectA", [new NuGetPackageInfo("NUnit", "1.2.3"), new NuGetPackageInfo("NUnit", "4.5.6")] }
        };

        var actual = Transform(packages);

        Assert.Single(actual);
        Assert.Equal("1.2.3", actual[0].Version);
    }

    [Fact]
    public void ToDistinctOrder_MultiplePackageIds_DifferentVersions_MinimumUsed()
    {
        var packages = new Dictionary<string, List<NuGetPackageInfo>>
        {
            {
                "ProjectA",
                [
                    new NuGetPackageInfo("Contoso.Utility.UsefulStuff", "17.9.0"),
                    new NuGetPackageInfo("Contoso.Utility.UsefulStuff", "16.3.6"),
                    new NuGetPackageInfo("Contoso.Utility.UsefulStuff", "18.0.0")
                ]
            }
        };

        var actual = Transform(packages);

        Assert.Single(actual);
        Assert.Equal("16.3.6", actual[0].Version);
    }

    [Fact]
    public void ToDistinctOrder_PackageIdsInAlphabeticalOrder_MultipleProjects()
    {
        var packages = new Dictionary<string, List<NuGetPackageInfo>>
        {
            {
                "project1",
                [
                    new NuGetPackageInfo("Newtonsoft.Json", "8.0.3"),
                    new NuGetPackageInfo("Some.Package", "1.0.0")
                ]
            },
            {
                "project2",
                [
                    new NuGetPackageInfo("jQuery", "3.1.1"),
                    new NuGetPackageInfo("RouteMagic", "1.3"),
                    new NuGetPackageInfo("Microsoft.Web.Xdt", "2.1.1")
                ]
            },
            {
                "project3",
                [
                    new NuGetPackageInfo("NuGet.Core", "2.11.1"),
                    new NuGetPackageInfo("Contoso.Utility.UsefulStuff", "17.9.0")
                ]
            }
        };

        var actual = Transform(packages);

        Assert.Equal(7, actual.Count);
        Assert.Equal("Contoso.Utility.UsefulStuff", actual[0].Id);
        Assert.Equal("jQuery", actual[1].Id);
        Assert.Equal("Microsoft.Web.Xdt", actual[2].Id);
        Assert.Equal("Newtonsoft.Json", actual[3].Id);
        Assert.Equal("NuGet.Core", actual[4].Id);
        Assert.Equal("RouteMagic", actual[5].Id);
        Assert.Equal("Some.Package", actual[6].Id);
    }

    [Fact]
    public void ToDistinctOrder_PackageIdsInAlphabeticalOrder_SingleProject()
    {
        var packages = new Dictionary<string, List<NuGetPackageInfo>>
        {
            {
                "project1",
                [
                    new NuGetPackageInfo("newtonsoft.json", "8.0.3"),
                    new NuGetPackageInfo("jQuery", "3.1.1"),
                    new NuGetPackageInfo("Some.Package", "1.0.0"),
                    new NuGetPackageInfo("NuGet.Core", "2.11.1"),
                    new NuGetPackageInfo("RouteMagic", "1.3"),
                    new NuGetPackageInfo("Contoso.Utility.UsefulStuff", "17.9.0"),
                    new NuGetPackageInfo("Microsoft.Web.Xdt", "2.1.1")
                ]
            }
        };

        var actual = Transform(packages);

        Assert.Equal(7, actual.Count);
        Assert.Equal("Contoso.Utility.UsefulStuff", actual[0].Id);
        Assert.Equal("jQuery", actual[1].Id);
        Assert.Equal("Microsoft.Web.Xdt", actual[2].Id);
        Assert.Equal("newtonsoft.json", actual[3].Id);
        Assert.Equal("NuGet.Core", actual[4].Id);
        Assert.Equal("RouteMagic", actual[5].Id);
        Assert.Equal("Some.Package", actual[6].Id);
    }

    private static List<NuGetPackageInfo> Transform(Dictionary<string, List<NuGetPackageInfo>> packages) =>
        packages.AsReadOnly().ToDistinctOrder().ToList();
}
