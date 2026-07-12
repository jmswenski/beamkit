using BeamKit.Esapi;

namespace BeamKit.Protocols.Acceptance;

/// <summary>
/// Optional ESAPI snapshot evidence gathered during package acceptance.
/// </summary>
public sealed record RtpxEsapiAcceptanceEvidence(
    string SnapshotPath,
    string CourseId,
    string PlanId,
    EsapiSnapshotValidationReport SnapshotValidation,
    IReadOnlyList<RtpxEsapiStructureCheck> StructureChecks,
    IReadOnlyList<RtpxEsapiPrescriptionCheck> PrescriptionChecks);
