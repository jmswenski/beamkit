namespace BeamKit.Naming;

/// <summary>
/// Creates a small TG-263-inspired starter dictionary.
/// </summary>
public static class Tg263SeedDictionaryFactory
{
    /// <summary>
    /// Creates a starter dictionary for demos and local policy bootstrapping.
    /// </summary>
    /// <remarks>
    /// This is not full TG-263 coverage and should not be treated as an authoritative copy of the TG-263 standard.
    /// </remarks>
    public static StructureNameDictionary CreateStarter()
    {
        var canonicalNames = new[]
        {
            "Body",
            "GTV",
            "CTV",
            "PTV",
            "PTV_7000",
            "SpinalCord",
            "Cord_PRV",
            "Brain",
            "Brainstem",
            "OpticChiasm",
            "OpticNrv_R",
            "OpticNrv_L",
            "Eye_R",
            "Eye_L",
            "Lens_R",
            "Lens_L",
            "Cochlea_R",
            "Cochlea_L",
            "Parotid_R",
            "Parotid_L",
            "Mandible",
            "OralCavity",
            "Larynx",
            "Esophagus",
            "Trachea",
            "Heart",
            "Lung_R",
            "Lung_L",
            "Lungs",
            "Liver",
            "Kidney_R",
            "Kidney_L",
            "Bladder",
            "Rectum",
            "Bowel",
            "FemurHead_R",
            "FemurHead_L"
        };
        var aliases = new[]
        {
            new StructureNameAlias("External", "Body", "TG-263-inspired starter"),
            new StructureNameAlias("Cord", "SpinalCord", "Common local alias"),
            new StructureNameAlias("Spinal Cord", "SpinalCord", "Common local alias"),
            new StructureNameAlias("Brain Stem", "Brainstem", "Common local alias"),
            new StructureNameAlias("Rt Lung", "Lung_R", "Common local alias"),
            new StructureNameAlias("Right Lung", "Lung_R", "Common local alias"),
            new StructureNameAlias("Lt Lung", "Lung_L", "Common local alias"),
            new StructureNameAlias("Left Lung", "Lung_L", "Common local alias"),
            new StructureNameAlias("Total Lung", "Lungs", "Common local alias"),
            new StructureNameAlias("Esoph", "Esophagus", "Common local alias"),
            new StructureNameAlias("Rt Kidney", "Kidney_R", "Common local alias"),
            new StructureNameAlias("Lt Kidney", "Kidney_L", "Common local alias"),
            new StructureNameAlias("Bowel Bag", "Bowel", "Common local alias")
        };
        var regexMappings = new[]
        {
            new StructureNameRegexMapping("^ptv[_ -]?(?<dose>[0-9]{2,4})$", "PTV_7000", "TG-263-inspired starter example"),
            new StructureNameRegexMapping("^gtv([_ -].*)?$", "GTV", "TG-263-inspired starter example"),
            new StructureNameRegexMapping("^ctv([_ -].*)?$", "CTV", "TG-263-inspired starter example")
        };

        return new StructureNameDictionary(
            "BeamKit TG-263-inspired starter",
            canonicalNames,
            aliases,
            regexMappings,
            id: "beamkit.tg263.starter",
            version: "0.1.0",
            description: "Starter dictionary for BeamKit demos and local policy bootstrapping. Not full TG-263 coverage.",
            source: "TG-263-inspired starter",
            tags: new[] { "starter", "tg-263-inspired", "synthetic" });
    }
}
