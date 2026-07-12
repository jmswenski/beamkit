using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using BeamKit.Protocols;

namespace BeamKit.Protocols.Word;

/// <summary>
/// Creates and inspects portable `.rtpx.zip` protocol packages derived from Word documents.
/// </summary>
public sealed class RtpxWordPackageStore
{
    /// <summary>
    /// RT-PX JSON entry stored at the root of the package.
    /// </summary>
    public const string RtpxEntryName = "rtpx.json";

    /// <summary>
    /// Package manifest entry stored at the root of the package.
    /// </summary>
    public const string ManifestEntryName = "manifest.json";

    /// <summary>
    /// Validation report entry stored at the root of the package.
    /// </summary>
    public const string ValidationEntryName = "validation-report.json";

    private const string SourceEntryPrefix = "source/";
    private const string PackageFormat = "beamkit.rtpx.word-package/0.1";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    private readonly RtpxWordProtocolExtractor extractor;

    /// <summary>
    /// Creates a package store.
    /// </summary>
    public RtpxWordPackageStore(RtpxWordProtocolExtractor? extractor = null)
    {
        this.extractor = extractor ?? new RtpxWordProtocolExtractor();
    }

    /// <summary>
    /// Extracts RT-PX from a Word document and writes a portable `.rtpx.zip` package when validation succeeds.
    /// </summary>
    public RtpxWordPackageResult Create(string docxPath, string outputPath, bool includeSourceDocument = false, bool overwrite = false)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("RT-PX package output path is required.", nameof(outputPath));
        }

        var fullOutputPath = Path.GetFullPath(outputPath);
        if (File.Exists(fullOutputPath) && !overwrite)
        {
            throw new IOException($"RT-PX package '{fullOutputPath}' already exists. Use --overwrite to replace it.");
        }

        var extraction = extractor.Extract(docxPath);
        if (!extraction.IsValid || extraction.Package is null || extraction.Validation is null)
        {
            return new RtpxWordPackageResult(fullOutputPath, extraction, null, WrotePackage: false);
        }

        var fullDocxPath = Path.GetFullPath(docxPath);
        var sourceFileName = Path.GetFileName(fullDocxPath);
        var files = new List<string>
        {
            RtpxEntryName,
            ManifestEntryName,
            ValidationEntryName
        };
        if (includeSourceDocument)
        {
            files.Add(SourceEntryPrefix + sourceFileName);
        }

        var manifest = new RtpxWordPackageManifest(
            PackageFormat,
            DateTimeOffset.UtcNow.ToString("O"),
            extraction.Package.Id,
            extraction.Package.Name,
            extraction.Package.Version,
            extraction.Package.SchemaVersion,
            sourceFileName,
            HashFile(fullDocxPath),
            includeSourceDocument,
            files);

        var directory = Path.GetDirectoryName(fullOutputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (File.Exists(fullOutputPath))
        {
            File.Delete(fullOutputPath);
        }

        using (var archive = ZipFile.Open(fullOutputPath, ZipArchiveMode.Create))
        {
            WriteTextEntry(archive, RtpxEntryName, RadiotherapyProtocolPackageStore.ToJson(extraction.Package));
            WriteTextEntry(archive, ManifestEntryName, JsonSerializer.Serialize(manifest, JsonOptions));
            WriteTextEntry(archive, ValidationEntryName, JsonSerializer.Serialize(extraction.Validation, JsonOptions));
            if (includeSourceDocument)
            {
                archive.CreateEntryFromFile(fullDocxPath, SourceEntryPrefix + sourceFileName, CompressionLevel.Optimal);
            }
        }

        return new RtpxWordPackageResult(fullOutputPath, extraction, manifest, WrotePackage: true);
    }

    /// <summary>
    /// Reads a portable `.rtpx.zip` package and returns its manifest, RT-PX package, and validation report.
    /// </summary>
    public RtpxWordPackageInspection Inspect(string packagePath)
    {
        if (string.IsNullOrWhiteSpace(packagePath))
        {
            throw new ArgumentException("RT-PX package path is required.", nameof(packagePath));
        }

        var fullPath = Path.GetFullPath(packagePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"RT-PX package '{fullPath}' was not found.", fullPath);
        }

        using var archive = ZipFile.OpenRead(fullPath);
        var entries = archive.Entries.Select(entry => entry.FullName).Order(StringComparer.Ordinal).ToArray();
        var package = RadiotherapyProtocolPackageStore.FromJson(ReadRequiredTextEntry(archive, RtpxEntryName));
        var manifest = JsonSerializer.Deserialize<RtpxWordPackageManifest>(ReadRequiredTextEntry(archive, ManifestEntryName), JsonOptions)
            ?? throw new InvalidOperationException("RT-PX package manifest could not be deserialized.");
        _ = ReadRequiredTextEntry(archive, ValidationEntryName);
        var validation = new RadiotherapyProtocolValidator().Validate(package);
        var sourceEntry = archive.GetEntry(SourceEntryPrefix + manifest.SourceFileName);
        bool? sourceHashVerified = null;
        if (sourceEntry is not null)
        {
            using var sourceStream = sourceEntry.Open();
            sourceHashVerified = string.Equals(HashStream(sourceStream), manifest.SourceHash, StringComparison.OrdinalIgnoreCase);
        }

        return new RtpxWordPackageInspection(fullPath, package, manifest, validation, entries, sourceHashVerified);
    }

    private static void WriteTextEntry(ZipArchive archive, string entryName, string contents)
    {
        var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open());
        writer.Write(contents);
    }

    private static string ReadRequiredTextEntry(ZipArchive archive, string entryName)
    {
        var entry = archive.GetEntry(entryName)
            ?? throw new InvalidOperationException($"RT-PX package is missing required entry '{entryName}'.");
        using var reader = new StreamReader(entry.Open());
        return reader.ReadToEnd();
    }

    private static string HashFile(string path)
    {
        using var stream = File.OpenRead(path);
        return HashStream(stream);
    }

    private static string HashStream(Stream stream)
    {
        return "sha256:" + Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }
}
