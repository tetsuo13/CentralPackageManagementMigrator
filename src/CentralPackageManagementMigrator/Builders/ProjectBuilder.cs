using System.Collections.ObjectModel;
using System.Xml;
using Microsoft.Extensions.Logging;

namespace CentralPackageManagementMigrator.Builders;

/// <summary>
/// Handles all interactions with the property files.
/// </summary>
internal class ProjectBuilder
{
    private const string VersionElementName = "Version";

    private readonly ILogger _logger;

    public ProjectBuilder(ILogger logger)
    {
        _logger = logger;
    }

    private static XmlDocument CreateXmlDocument() => new()
    {
        PreserveWhitespace = true
    };

    /// <summary>
    /// Recursively search for projects starting at a base directory to collect
    /// all packages. Note that duplicates are not omitted.
    /// </summary>
    /// <param name="searchPath">The base directory to search for project files under.</param>
    /// <returns>
    /// Dictionary of project file paths to a collection of packages found in
    /// that project.
    /// </returns>
    public ReadOnlyDictionary<string, List<NuGetPackageInfo>> GetPackagesInAllProjects(string searchPath)
    {
        const string searchPattern = "*.csproj";

        _logger.LogInformation("Finding all {SearchPattern} files under: {SearchPath}", searchPattern, searchPath);
        var allProjects = Directory.GetFiles(searchPath, searchPattern, SearchOption.AllDirectories);
        _logger.LogDebug("Found {Count} files", allProjects.Length);

        var allPackages = new Dictionary<string, List<NuGetPackageInfo>>();

        _logger.LogInformation("Reading project files");

        foreach (var projectFile in allProjects)
        {
            _logger.LogInformation("Reading project {FileName} for PackageReferences", projectFile);

            var projectDocument = CreateXmlDocument();
            projectDocument.Load(projectFile);
            _logger.LogDebug("Project XML loaded");

            var packagesInProject = GetPackagesInProject(projectDocument);

            if (packagesInProject.Count > 0)
            {
                allPackages.Add(projectFile, packagesInProject);
            }
            else
            {
                _logger.LogDebug("No PackageReferences found in {FileName}", projectFile);
            }
        }

        return allPackages.AsReadOnly();
    }

    /// <summary>
    /// For unit tests.
    /// </summary>
    internal HashSet<string> GetTargetFrameworksFromSource(string projectSource)
    {
        var doc = CreateXmlDocument();
        doc.LoadXml(projectSource);
        var frameworks = new HashSet<string>();

        var single = doc.SelectSingleNode("//TargetFramework");
        if (single is not null)
        {
            frameworks.Add(single.InnerText.Trim());
        }

        var multiple = doc.SelectSingleNode("//TargetFrameworks");
        if (multiple is not null)
        {
            foreach (var tf in multiple.InnerText.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                frameworks.Add(tf);
            }
        }

        return frameworks;
    }

    public HashSet<string> GetTargetFrameworks(string searchPath)
    {
        const string searchPattern = "*.csproj";

        _logger.LogInformation("Collecting target frameworks from {SearchPattern} files under: {SearchPath}",
            searchPattern, searchPath);
        var allProjects = Directory.GetFiles(searchPath, searchPattern, SearchOption.AllDirectories);

        var frameworks = new HashSet<string>();

        foreach (var projectFile in allProjects)
        {
            var doc = CreateXmlDocument();
            doc.Load(projectFile);

            var single = doc.SelectSingleNode("//TargetFramework");
            if (single is not null)
            {
                frameworks.Add(single.InnerText.Trim());
                continue;
            }

            var multiple = doc.SelectSingleNode("//TargetFrameworks");
            if (multiple is not null)
            {
                foreach (var tf in multiple.InnerText.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    frameworks.Add(tf);
                }
            }
        }

        return frameworks;
    }

    /// <summary>
    /// For unit tests.
    /// </summary>
    internal List<NuGetPackageInfo> GetPackagesProjectSource(string projectSource)
    {
        var projectDocument = CreateXmlDocument();
        projectDocument.LoadXml(projectSource);
        return GetPackagesInProject(projectDocument);
    }

    /// <summary>
    /// For unit tests.
    /// </summary>
    internal string UpdateProjectFromSource(string projectSource, List<NuGetPackageInfo> packages)
    {
        var doc = CreateXmlDocument();
        doc.LoadXml(projectSource);
        RemoveVersionOnPackageReferences(doc, packages);
        return doc.OuterXml;
    }

    private void RemoveVersionOnPackageReferences(XmlDocument doc, List<NuGetPackageInfo> packages)
    {
        foreach (var packageId in packages.Select(x => x.Id).Distinct())
        {
            _logger.LogDebug("Locating PackageReferences for Include = {PackageId}", packageId);

            var matches = doc.SelectNodes($"//PackageReference[@Include='{packageId}']");

            if (matches is null || matches.Count == 0)
            {
                _logger.LogInformation("Couldn't find any elements");
                continue;
            }

            foreach (XmlNode match in matches)
            {
                RemoveVersionFromReference((XmlElement)match, packageId);
            }
        }
    }

    private static void RemoveVersionFromReference(XmlElement packageReference, string packageId)
    {
        var removedVersion = packageReference.Attributes?.Remove(packageReference.Attributes[VersionElementName]);

        if (removedVersion is not null)
        {
            return;
        }

        var versionElement = packageReference.SelectSingleNode(VersionElementName);

        if (versionElement is null)
        {
            return;
        }

        packageReference.RemoveChild(versionElement);

        foreach (var el in packageReference.ChildNodes.OfType<XmlWhitespace>())
        {
            packageReference.RemoveChild(el);
        }

        if (string.IsNullOrWhiteSpace(packageReference.InnerText))
        {
            packageReference.InnerXml = string.Empty;
            packageReference.IsEmpty = true;
        }
    }

    public void UpdateProjects(ReadOnlyDictionary<string, List<NuGetPackageInfo>> projectPackages)
    {
        _logger.LogInformation("Updating project files PackageReference elements");

        foreach (var projectPackage in projectPackages)
        {
            _logger.LogInformation("Updating project {PackageName}", projectPackage.Key);

            var doc = CreateXmlDocument();
            doc.Load(projectPackage.Key);
            _logger.LogDebug("Project XML loaded");

            RemoveVersionOnPackageReferences(doc, projectPackage.Value);

            _logger.LogDebug("Saving file");
            doc.Save(projectPackage.Key);
        }
    }

    private List<NuGetPackageInfo> GetPackagesInProject(XmlDocument projectDocument)
    {
        _logger.LogDebug("Checking for PackageReferences");
        var packageReferences = projectDocument.SelectNodes("//PackageReference");
        _logger.LogInformation("Found {Count} PackageReferences", packageReferences?.Count ?? 0);

        if (packageReferences is null || packageReferences.Count < 1)
        {
            return [];
        }

        return GetPackagesFromReferences(packageReferences);
    }

    private List<NuGetPackageInfo> GetPackagesFromReferences(XmlNodeList packageReferences)
    {
        var packagesInProject = new List<NuGetPackageInfo>();

        foreach (XmlNode packageReference in packageReferences)
        {
            _logger.LogDebug("Checking Include and Version attributes on PackageReference");

            var packageName = packageReference.Attributes?["Include"]?.Value;
            var packageVersion = packageReference.Attributes?[VersionElementName]?.Value;

            if (string.IsNullOrEmpty(packageName))
            {
                _logger.LogWarning("Found null package name");
                continue;
            }

            if (string.IsNullOrEmpty(packageVersion))
            {
                _logger.LogDebug("No Version attribute found");

                var version = packageReference.SelectSingleNode(VersionElementName);

                if (version is null)
                {
                    _logger.LogWarning("No Version child element either");
                    continue;
                }

                packageVersion = version.InnerText;
            }

            var condition = (packageReference.ParentNode as XmlElement)?.GetAttribute("Condition");
            var conditionValue = string.IsNullOrEmpty(condition) ? null : condition;

            _logger.LogDebug("Found NuGet package {PackageName} version {PackageVersion}{Condition}",
                packageName, packageVersion,
                conditionValue is not null ? $" condition: {conditionValue}" : "");

            packagesInProject.Add(new NuGetPackageInfo(packageName, packageVersion, conditionValue));
        }

        return packagesInProject;
    }
}
