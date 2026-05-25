using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CentralPackageManagementMigrator.Builders;

namespace CentralPackageManagementMigrator;

internal static class NuGetPackageInfoExtensions
{
    public static IEnumerable<NuGetPackageInfo> ToDistinctOrder(
        this ReadOnlyDictionary<string, List<NuGetPackageInfo>> packages,
        HashSet<string> allTargetFrameworks)
    {
        var allEntries = packages.SelectMany(x => x.Value).Distinct();

        var grouped = allEntries
            .GroupBy(x => (x.Id, x.Condition))
            .Select(MinimumPackageVersion)
            .ToList();

        return PromoteConditional(grouped, allTargetFrameworks)
            .OrderBy(x => x.Id, StringComparer.InvariantCultureIgnoreCase);
    }

    private static NuGetPackageInfo MinimumPackageVersion(
        IGrouping<(string Id, string? Condition), NuGetPackageInfo> arg)
    {
        return arg.OrderBy(x => x.Version).First();
    }

    internal static List<NuGetPackageInfo> PromoteConditional(
        List<NuGetPackageInfo> packages,
        HashSet<string> allTargetFrameworks)
    {
        var result = new List<NuGetPackageInfo>();

        foreach (var idGroup in packages.GroupBy(x => x.Id))
        {
            var unconditional = idGroup.FirstOrDefault(x => x.Condition is null);

            if (unconditional is not null)
            {
                result.Add(unconditional);
                continue;
            }

            var conditional = idGroup.ToList();
            var coveredTfs = conditional
                .Select(x => ExtractTargetFramework(x.Condition))
                .Where(x => x is not null)
                .Cast<string>()
                .ToHashSet();

            if (coveredTfs.Count > 0 && coveredTfs.IsSupersetOf(allTargetFrameworks))
            {
                var lowestVersion = conditional.OrderBy(x => x.Version).First();
                result.Add(new NuGetPackageInfo(lowestVersion.Id, lowestVersion.Version, null));
            }
            else
            {
                result.AddRange(conditional);
            }
        }

        return result;
    }

    internal static string? ExtractTargetFramework(string? condition)
    {
        if (string.IsNullOrEmpty(condition))
        {
            return null;
        }

        var match = Regex.Match(condition, @"==\s*'([^']+)'");
        return match.Success ? match.Groups[1].Value : null;
    }
}
