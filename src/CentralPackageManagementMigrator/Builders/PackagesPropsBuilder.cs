using System.Text;
using System.Xml;
using Microsoft.Extensions.Logging;

namespace CentralPackageManagementMigrator.Builders;

/// <summary>
/// Handles all interactions with the <i>Directory.Packages.props</i> file.
/// </summary>
internal class PackagesPropsBuilder
{
    /// <summary>
    /// The packages directory properties file name.
    /// </summary>
    public static string TargetFileName => "Directory.Packages.props";
    public bool Exists => File.Exists(_filePath);

    private readonly ILogger _logger;
    private readonly string _filePath;

    public PackagesPropsBuilder(ILogger logger, string filePath)
    {
        _logger = logger;
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _filePath = Path.Combine(filePath, TargetFileName);
    }

    public void WriteFile(IEnumerable<NuGetPackageInfo> packages)
    {
        _logger.LogInformation("Writing file to {FilePath}", _filePath);

        var doc = GenerateDocument(packages);
        SaveDocument(doc);
    }

    private static XmlWriterSettings CreateXmlWriterSettings() => new()
    {
        Indent = true,
        IndentChars = "  ",
        OmitXmlDeclaration = true
    };

    /// <summary>
    /// For unit tests. Does everything <see cref="WriteFile"/> does except
    /// it returns a string of the generated XML document instead of writing
    /// to disk.
    /// </summary>
    internal string GenerateXml(IEnumerable<NuGetPackageInfo> packages)
    {
        var doc = GenerateDocument(packages);
        var stringWriter = new StringBuilder();
        using var xmlWriter = XmlWriter.Create(stringWriter, CreateXmlWriterSettings());
        doc.Save(xmlWriter);
        return stringWriter.ToString();
    }

    private void SaveDocument(XmlDocument doc)
    {
        _logger.LogDebug("Saving file");

        using var stream = new FileStream(_filePath, FileMode.Create, FileAccess.Write);
        using var writer = XmlWriter.Create(stream, CreateXmlWriterSettings());
        doc.WriteTo(writer);
    }

    private XmlDocument GenerateDocument(IEnumerable<NuGetPackageInfo> packages)
    {
        _logger.LogDebug("Generating XML document");
        var doc = new XmlDocument();

        var project = doc.CreateElement(string.Empty, "Project", string.Empty);
        doc.AppendChild(project);

        var propertyGroup = doc.CreateElement(string.Empty, "PropertyGroup", string.Empty);

        var managePackageVersionsCentrally = doc.CreateElement(string.Empty, "ManagePackageVersionsCentrally", string.Empty);
        var value = doc.CreateTextNode("true");
        managePackageVersionsCentrally.AppendChild(value);
        propertyGroup.AppendChild(managePackageVersionsCentrally);
        project.AppendChild(propertyGroup);

        var itemGroup = doc.CreateElement(string.Empty, "ItemGroup", string.Empty);

        foreach (var package in packages)
        {
            var packageVersion = doc.CreateElement(string.Empty, "PackageVersion", string.Empty);
            packageVersion.SetAttribute("Include", package.Id);
            packageVersion.SetAttribute("Version", package.Version);

            itemGroup.AppendChild(packageVersion);
        }

        project.AppendChild(itemGroup);

        return doc;
    }
}
