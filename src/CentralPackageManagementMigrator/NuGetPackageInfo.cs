namespace CentralPackageManagementMigrator;

internal record NuGetPackageInfo
{
    public string Id { get; }
    public string Version { get; }

    public NuGetPackageInfo(string id, string version)
    {
        Id = id;
        Version = version;
    }
}
