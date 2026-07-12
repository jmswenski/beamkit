using BeamKit.RulePacks;

namespace BeamKit.Protocols;

/// <summary>
/// Rule-pack scaffold generated from an RT-PX package.
/// </summary>
public sealed record RadiotherapyProtocolCompilation
{
    /// <summary>
    /// Creates a protocol compilation result.
    /// </summary>
    public RadiotherapyProtocolCompilation(
        RadiotherapyProtocolPackage package,
        ProtocolValidationReport validation,
        RulePackScaffold scaffold)
    {
        Package = package ?? throw new ArgumentNullException(nameof(package));
        Validation = validation ?? throw new ArgumentNullException(nameof(validation));
        Scaffold = scaffold ?? throw new ArgumentNullException(nameof(scaffold));
    }

    /// <summary>
    /// Source RT-PX package.
    /// </summary>
    public RadiotherapyProtocolPackage Package { get; init; }

    /// <summary>
    /// Validation report generated before compilation.
    /// </summary>
    public ProtocolValidationReport Validation { get; init; }

    /// <summary>
    /// Generated BeamKit rule-pack scaffold.
    /// </summary>
    public RulePackScaffold Scaffold { get; init; }

    /// <summary>
    /// Relative path to the generated rule-pack manifest.
    /// </summary>
    public string ManifestPath => Scaffold.ManifestPath;

    /// <summary>
    /// Number of files generated in the rule-pack scaffold.
    /// </summary>
    public int FileCount => Scaffold.Files.Count;
}
