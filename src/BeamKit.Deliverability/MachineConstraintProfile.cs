using System.Text.Json;
using System.Text.Json.Serialization;

namespace BeamKit.Deliverability;

/// <summary>
/// Versioned machine and technique constraints for deliverability checks.
/// </summary>
public sealed record MachineConstraintProfile
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    /// <summary>
    /// Creates a machine constraint profile.
    /// </summary>
    public MachineConstraintProfile(
        string name,
        string version,
        string? machineId = null,
        string? energy = null,
        decimal? minMonitorUnitsPerDegree = null,
        decimal? minMonitorUnitsPerSegment = null,
        decimal? minMonitorUnitsPerBeam = null,
        decimal? maxOpenFieldSizeCm = null,
        decimal? maxMlcFieldSizeCm = null,
        decimal? maxFffFieldSizeCm = null,
        decimal? maxDcaStepSizeDegrees = null,
        string? beamModelId = null,
        string? calculationModel = null,
        string? calculationModelVersion = null,
        decimal? minJawOpeningCm = null,
        bool? requireJawTracking = null,
        IReadOnlyList<string>? allowedEnergies = null,
        IReadOnlyList<string>? allowedTechniques = null,
        IReadOnlyList<string>? allowedBeamModelIds = null,
        IReadOnlyList<MonitorUnitsPerDegreeConstraint>? monitorUnitsPerDegreeConstraints = null)
    {
        Name = DeliverabilityText.Required(name, nameof(name));
        Version = DeliverabilityText.Required(version, nameof(version));
        MachineId = string.IsNullOrWhiteSpace(machineId) ? null : machineId.Trim();
        Energy = string.IsNullOrWhiteSpace(energy) ? null : energy.Trim();
        MinMonitorUnitsPerDegree = minMonitorUnitsPerDegree;
        MinMonitorUnitsPerSegment = minMonitorUnitsPerSegment;
        MinMonitorUnitsPerBeam = minMonitorUnitsPerBeam;
        MaxOpenFieldSizeCm = maxOpenFieldSizeCm;
        MaxMlcFieldSizeCm = maxMlcFieldSizeCm;
        MaxFffFieldSizeCm = maxFffFieldSizeCm;
        MaxDcaStepSizeDegrees = maxDcaStepSizeDegrees;
        BeamModelId = string.IsNullOrWhiteSpace(beamModelId) ? null : beamModelId.Trim();
        CalculationModel = string.IsNullOrWhiteSpace(calculationModel) ? null : calculationModel.Trim();
        CalculationModelVersion = string.IsNullOrWhiteSpace(calculationModelVersion) ? null : calculationModelVersion.Trim();
        MinJawOpeningCm = minJawOpeningCm;
        RequireJawTracking = requireJawTracking;
        AllowedEnergies = NormalizeList(allowedEnergies);
        AllowedTechniques = NormalizeList(allowedTechniques);
        AllowedBeamModelIds = NormalizeList(allowedBeamModelIds);
        MonitorUnitsPerDegreeConstraints = monitorUnitsPerDegreeConstraints?.ToArray() ?? Array.Empty<MonitorUnitsPerDegreeConstraint>();
    }

    /// <summary>
    /// Profile name.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Profile version.
    /// </summary>
    public string Version { get; init; }

    /// <summary>
    /// Optional machine id.
    /// </summary>
    public string? MachineId { get; init; }

    /// <summary>
    /// Optional energy selector.
    /// </summary>
    public string? Energy { get; init; }

    /// <summary>
    /// Minimum monitor units per degree for arcs.
    /// </summary>
    public decimal? MinMonitorUnitsPerDegree { get; init; }

    /// <summary>
    /// Minimum monitor units per segment or control-point interval.
    /// </summary>
    public decimal? MinMonitorUnitsPerSegment { get; init; }

    /// <summary>
    /// Minimum monitor units per treatment beam.
    /// </summary>
    public decimal? MinMonitorUnitsPerBeam { get; init; }

    /// <summary>
    /// Maximum jaw-defined open field dimension in centimeters.
    /// </summary>
    public decimal? MaxOpenFieldSizeCm { get; init; }

    /// <summary>
    /// Maximum MLC-shaped field dimension in centimeters.
    /// </summary>
    public decimal? MaxMlcFieldSizeCm { get; init; }

    /// <summary>
    /// Maximum FFF field dimension in centimeters.
    /// </summary>
    public decimal? MaxFffFieldSizeCm { get; init; }

    /// <summary>
    /// Maximum DCA gantry step size in degrees.
    /// </summary>
    public decimal? MaxDcaStepSizeDegrees { get; init; }

    /// <summary>
    /// Expected beam model identifier, when one model applies to the profile.
    /// </summary>
    public string? BeamModelId { get; init; }

    /// <summary>
    /// Expected dose calculation model or algorithm.
    /// </summary>
    public string? CalculationModel { get; init; }

    /// <summary>
    /// Expected dose calculation model or algorithm version.
    /// </summary>
    public string? CalculationModelVersion { get; init; }

    /// <summary>
    /// Minimum jaw opening in centimeters for both X and Y dimensions.
    /// </summary>
    public decimal? MinJawOpeningCm { get; init; }

    /// <summary>
    /// Required jaw-tracking state, when policy specifies it.
    /// </summary>
    public bool? RequireJawTracking { get; init; }

    /// <summary>
    /// Allowed beam energy labels.
    /// </summary>
    public IReadOnlyList<string> AllowedEnergies { get; init; }

    /// <summary>
    /// Allowed beam technique identifiers.
    /// </summary>
    public IReadOnlyList<string> AllowedTechniques { get; init; }

    /// <summary>
    /// Allowed beam model identifiers.
    /// </summary>
    public IReadOnlyList<string> AllowedBeamModelIds { get; init; }

    /// <summary>
    /// Optional keyed MU/degree constraints. Most-specific matching rule wins.
    /// </summary>
    public IReadOnlyList<MonitorUnitsPerDegreeConstraint> MonitorUnitsPerDegreeConstraints { get; init; }

    /// <summary>
    /// Creates a conservative synthetic profile for demos and tests.
    /// </summary>
    public static MachineConstraintProfile CreateSynthetic()
    {
        return new MachineConstraintProfile(
            "Synthetic linear accelerator constraints",
            "2026.1",
            machineId: "SYN-LINAC",
            beamModelId: "SYN-AAA-6X",
            calculationModel: "SyntheticAAA",
            calculationModelVersion: "16.1",
            minMonitorUnitsPerDegree: 0.1m,
            minMonitorUnitsPerSegment: 0.1m,
            minMonitorUnitsPerBeam: 40m,
            maxOpenFieldSizeCm: 40m,
            maxMlcFieldSizeCm: 22m,
            maxFffFieldSizeCm: 15m,
            maxDcaStepSizeDegrees: 5m,
            minJawOpeningCm: 0.5m,
            requireJawTracking: true,
            allowedEnergies: new[] { "6X" },
            allowedTechniques: new[] { "VMAT" },
            allowedBeamModelIds: new[] { "SYN-AAA-6X" },
            monitorUnitsPerDegreeConstraints: new[]
            {
                new MonitorUnitsPerDegreeConstraint(
                    0.1m,
                    machineId: "SYN-LINAC",
                    energy: "6X",
                    techniqueId: "VMAT",
                    diseaseSite: "Head and Neck")
            });
    }

    /// <summary>
    /// Loads a machine constraint profile from JSON.
    /// </summary>
    public static MachineConstraintProfile FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("JSON is required.", nameof(json));
        }

        return JsonSerializer.Deserialize<MachineConstraintProfile>(json, Options)
            ?? throw new InvalidOperationException("Machine constraint profile JSON did not produce a profile.");
    }

    /// <summary>
    /// Loads a machine constraint profile from a JSON file.
    /// </summary>
    public static MachineConstraintProfile FromFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path is required.", nameof(path));
        }

        return FromJson(File.ReadAllText(path));
    }

    private static IReadOnlyList<string> NormalizeList(IReadOnlyList<string>? values)
    {
        return values?
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray()
            ?? Array.Empty<string>();
    }
}
