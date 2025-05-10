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
    public void Equality(string leftId, string leftVersion, string rightId, string rightVersion, bool expected)
    {
        var left = new NuGetPackageInfo(leftId, leftVersion);
        var right = new NuGetPackageInfo(rightId, rightVersion);
        Assert.Equal(expected, left.Equals(right));
    }
}
