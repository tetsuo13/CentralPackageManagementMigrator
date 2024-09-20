using System.Collections.ObjectModel;
using System.Xml;
using Microsoft.Extensions.Logging;

namespace CentralPackageManagementMigrator.Builders;

/// <summary>
/// Handles all interactions with the property files.
/// </summary>
internal class ProjectBuilder
{
    private readonly ILogger _logger;

    public ProjectBuilder(ILogger logger)
    {
        _logger = logger;
    }

    private static XmlDocument CreateXmlDocument() => new()
    {
        PreserveWhitespace = true
    };

    public ReadOnlyDictionary<string, ReadOnlyCollection<NuGetPackageInfo>> GetPackagesInAllProjects(string searchPath)
    {
        const string searchPattern = "*.csproj";

        _logger.LogInformation("Finding all {SearchPattern} files under: {SearchPath}", searchPattern, searchPath);
        var allProjects = Directory.GetFiles(searchPath, searchPattern, SearchOption.AllDirectories);
        _logger.LogDebug("Found {Count} files", allProjects.Length);

        var allPackages = new Dictionary<string, ReadOnlyCollection<NuGetPackageInfo>>();

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
    internal ReadOnlyCollection<NuGetPackageInfo> GetPackagesProjectSource(string projectSource)
    {
        var projectDocument = CreateXmlDocument();
        projectDocument.LoadXml(projectSource);
        return GetPackagesInProject(projectDocument);
    }

    /// <summary>
    /// For unit tests.
    /// </summary>
    internal string UpdateProjectFromSource(string projectSource, ReadOnlyCollection<NuGetPackageInfo> packages)
    {
        var doc = CreateXmlDocument();
        doc.LoadXml(projectSource);
        RemoveVersionOnPackageReferences(doc, packages);
        return doc.OuterXml;
    }

    private void RemoveVersionOnPackageReferences(XmlDocument doc, ReadOnlyCollection<NuGetPackageInfo> packages)
    {
        foreach (var package in packages)
        {
            _logger.LogDebug("Locating single PackageReference for Includes = {PackageId}", package.Id);
            var packageReference = doc.SelectSingleNode($"//PackageReference[@Include='{package.Id}']");

            // TODO: Should we do something here? Assumes it should always be found. Why wouldn't it though?
            _logger.LogDebug("Found element = {ElementFound}", packageReference is not null);

            packageReference?.Attributes?.Remove(packageReference.Attributes["Version"]);
            _logger.LogInformation("Removed Version attribute for package {PackageId}", package.Id);
        }
    }

    public void UpdateProjects(ReadOnlyDictionary<string, ReadOnlyCollection<NuGetPackageInfo>> projectPackages)
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

    private ReadOnlyCollection<NuGetPackageInfo> GetPackagesInProject(XmlDocument projectDocument)
    {
        _logger.LogDebug("Checking for PackageReferences");
        var packageReferences = projectDocument.SelectNodes("//PackageReference");
        _logger.LogInformation("Found {Count} PackageReferences", packageReferences?.Count ?? 0);

        if (packageReferences is null || packageReferences.Count < 1)
        {
            return ReadOnlyCollection<NuGetPackageInfo>.Empty;
        }

        return GetPackagesFromReferences(packageReferences);
    }

    private ReadOnlyCollection<NuGetPackageInfo> GetPackagesFromReferences(XmlNodeList packageReferences)
    {
        var packagesInProject = new List<NuGetPackageInfo>();

        foreach (XmlNode packageReference in packageReferences)
        {
            _logger.LogDebug("Checking Include and Version attributes on PackageReference");

            var packageName = packageReference.Attributes?["Include"]?.Value;
            var packageVersion = packageReference.Attributes?["Version"]?.Value;

            if (string.IsNullOrEmpty(packageVersion))
            {
                _logger.LogWarning("Detected null package version");
                continue;
            }

            if (string.IsNullOrEmpty(packageName))
            {
                _logger.LogWarning("Found null package name");
                continue;
            }

            _logger.LogDebug("Found NuGet package {PackageName} version {PackageVersion}",
                packageName, packageVersion);

            packagesInProject.Add(new NuGetPackageInfo(packageName, packageVersion));
        }

        return packagesInProject.AsReadOnly();
    }
}
