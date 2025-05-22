using System.Collections.ObjectModel;
using CentralPackageManagementMigrator.Builders;

namespace CentralPackageManagementMigrator;

/// <summary>
/// Intermediary extensions used by Command class to prepare data structures
/// between builders.
/// </summary>
internal static class NuGetPackageInfoExtensions
{
    /// <summary>
    /// Extracts all collections of NuGetPackageInfo objects to return a
    /// collection of distinct NuGetPackageInfo objects in order by name.
    /// </summary>
    /// <param name="packages">
    /// The return from <see cref="ProjectBuilder.GetPackagesInAllProjects"/>,
    /// a dictionary of each project and the packages used.
    /// </param>
    /// <returns></returns>
    public static IEnumerable<NuGetPackageInfo> ToDistinctOrder(
        this ReadOnlyDictionary<string, List<NuGetPackageInfo>> packages)
    {
        return packages.SelectMany(x => x.Value)

            // Removes duplicates as defined by the implementation of
            // IEquatable in NuGetPackageInfo.
            .Distinct()

            // If there are multiple versions of the same package, keeps only
            // a single one.
            .GroupBy(x => x.Id)
            .Select(MinimumPackageVersion)

            // Order by the package names.
            .OrderBy(x => x.Id, StringComparer.InvariantCultureIgnoreCase);
    }

    /// <summary>
    /// Finds the instance with the "lowest" version.
    /// </summary>
    /// <param name="arg">A grouping of instances with the same package ID.</param>
    /// <returns>The instance with the lowest version.</returns>
    private static NuGetPackageInfo MinimumPackageVersion(IGrouping<string, NuGetPackageInfo> arg)
    {
        // A naive algorithm. This will likely need to be reworked some day
        // when encountering more complex examples.
        return arg.OrderBy(x => x.Version).First();
    }
}
