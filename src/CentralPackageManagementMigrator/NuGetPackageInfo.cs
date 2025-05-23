namespace CentralPackageManagementMigrator;

internal class NuGetPackageInfo : IEquatable<NuGetPackageInfo>
{
    public string Id { get; }
    public string Version { get; }

    public NuGetPackageInfo(string id, string version)
    {
        Id = id;
        Version = version;
    }

    public override bool Equals(object? obj) => Equals(obj as NuGetPackageInfo);
    public bool Equals(NuGetPackageInfo? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Id.Equals(other.Id, StringComparison.InvariantCultureIgnoreCase) &&
               Version.Equals(other.Version);
    }

    public override int GetHashCode() => HashCode.Combine(Id.ToLowerInvariant(), Version);
}
