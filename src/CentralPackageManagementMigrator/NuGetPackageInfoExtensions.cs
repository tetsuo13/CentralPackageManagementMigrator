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
            .GroupBy(x => (x.Id.ToLowerInvariant(), x.Condition))
            .Select(MaximumPackageVersion)
            .ToList();

        return PromoteConditional(grouped, allTargetFrameworks)
            .OrderBy(x => x.Id, StringComparer.InvariantCultureIgnoreCase);
    }

    private static NuGetPackageInfo MaximumPackageVersion(
        IGrouping<(string Id, string? Condition), NuGetPackageInfo> arg)
    {
        return arg.OrderByDescending(x => x.Version).First();
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
                var conditionalEntries = idGroup.Where(x => x.Condition is not null).ToList();

                if (conditionalEntries.Count == 0 || conditionalEntries.All(x => x.Version == unconditional.Version))
                {
                    result.Add(unconditional);
                }
                else
                {
                    result.AddRange(conditionalEntries);
                }

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
                var distinctVersions = conditional.Select(x => x.Version).Distinct().ToList();
                if (distinctVersions.Count == 1)
                {
                    result.Add(new NuGetPackageInfo(conditional[0].Id, distinctVersions[0], null));
                }
                else
                {
                    result.AddRange(conditional);
                }
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
