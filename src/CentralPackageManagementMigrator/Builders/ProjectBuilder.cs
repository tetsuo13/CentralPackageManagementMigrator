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
        foreach (var packageId in packages.Select(x => x.Id))
        {
            _logger.LogDebug("Locating single PackageReference for Includes = {PackageId}", packageId);

            if (doc.SelectSingleNode($"//PackageReference[@Include='{packageId}']") is not XmlElement packageReference)
            {
                _logger.LogInformation("Couldn't find any elements");
                continue;
            }

            var removedVersion = packageReference.Attributes?.Remove(packageReference.Attributes[VersionElementName]);

            if (removedVersion is not null)
            {
                _logger.LogInformation("Removed Version attribute for package {PackageId}", packageId);
                continue;
            }

            _logger.LogDebug("No Version attribute found, looking for child element");
            var versionElement = packageReference.SelectSingleNode(VersionElementName);

            if (versionElement is null)
            {
                _logger.LogInformation("No Version attribute and no Version child element for package {PackageId}",
                    packageId);
                continue;
            }

            packageReference.RemoveChild(versionElement);

            // A Version child element means at least two children: one for
            // whitespace before the element and then the element itself.
            // Remove the leading whitespace.
            foreach (var el in packageReference.ChildNodes.OfType<XmlWhitespace>())
            {
                packageReference.RemoveChild(el);
            }

            // If the PackageReference element only contained a Version
            // element, then there will be an additional whitespace child
            // element that precedes the closing PackageReference element. In
            // this case, remove all whitespace and mark the element as
            // self-closing.
            if (string.IsNullOrWhiteSpace(packageReference.InnerText))
            {
                packageReference.InnerXml = string.Empty;
                packageReference.IsEmpty = true;
            }
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

            _logger.LogDebug("Found NuGet package {PackageName} version {PackageVersion}",
                packageName, packageVersion);

            packagesInProject.Add(new NuGetPackageInfo(packageName, packageVersion));
        }

        return packagesInProject;
    }
}
