namespace CentralPackageManagementMigrator.IntegrationTests;

public class IntegrationTests
{
    [Fact]
    public async Task Test001_BasicExample()
    {
        using var helper = Helper.Create("Test001");

        var exitCode = await helper.RunMigrator();

        Assert.Equal(0, exitCode);
        await helper.AssertDirectoryPackagesFile();
        await helper.AssertProjectFile("Project");
    }

    [Fact]
    public async Task Test002_PackageVersionAsChildElement()
    {
        using var helper = Helper.Create("Test002");

        var exitCode = await helper.RunMigrator();

        Assert.Equal(0, exitCode);
        await helper.AssertDirectoryPackagesFile();
        await helper.AssertProjectFile("Project");
    }

    [Fact]
    public async Task Test003_PackageNamesCaseInsensitive()
    {
        using var helper = Helper.Create("Test003");

        var exitCode = await helper.RunMigrator();

        Assert.Equal(0, exitCode);
        await helper.AssertDirectoryPackagesFile();
        await helper.AssertProjectFile("ProjectA");
        await helper.AssertProjectFile("ProjectB");
    }

    [Fact]
    public async Task Test004_DirectoryPackagesPropsAlreadyExists()
    {
        using var helper = Helper.Create("Test004");

        var exitCode = await helper.RunMigrator();

        Assert.Equal(1, exitCode);
        await helper.AssertProjectFile("Project");
    }
}
