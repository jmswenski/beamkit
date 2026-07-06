using BeamKit.Naming;

namespace BeamKit.Samples;

/// <summary>
/// Creates synthetic structure-name dictionaries for tests, demos, and documentation.
/// </summary>
public static class SyntheticStructureNameDictionaryFactory
{
    /// <summary>
    /// Creates a small TG-263-inspired synthetic dictionary for Milestone 3 demos.
    /// </summary>
    public static StructureNameDictionary CreateTg263Subset()
    {
        var canonicalNames = new[]
        {
            "Body",
            "PTV_7000",
            "CTV",
            "GTV",
            "SpinalCord",
            "Cord_PRV",
            "Heart",
            "Lung_R",
            "Lung_L",
            "Lungs",
            "Esophagus",
            "Trachea",
            "BrachialPlex_R",
            "BrachialPlex_L",
            "Brain",
            "Brainstem",
            "OpticNrv_R",
            "OpticNrv_L",
            "OpticChiasm",
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
            "Pharynx",
            "Bladder",
            "Rectum",
            "Bowel",
            "FemurHead_R",
            "FemurHead_L"
        };

        var aliases = new[]
        {
            new StructureNameAlias("External", "Body", "TG-263 subset"),
            new StructureNameAlias("BODY", "Body", "Institution alias"),
            new StructureNameAlias("Cord", "SpinalCord", "Institution alias"),
            new StructureNameAlias("Spinal Cord", "SpinalCord", "Institution alias"),
            new StructureNameAlias("Cord PRV", "Cord_PRV", "Institution alias"),
            new StructureNameAlias("SpinalCord_PRV", "Cord_PRV", "Institution alias"),
            new StructureNameAlias("Rt Lung", "Lung_R", "Institution alias"),
            new StructureNameAlias("Right Lung", "Lung_R", "Institution alias"),
            new StructureNameAlias("R Lung", "Lung_R", "Institution alias"),
            new StructureNameAlias("Lung_Right", "Lung_R", "Institution alias"),
            new StructureNameAlias("LungR", "Lung_R", "Institution alias"),
            new StructureNameAlias("Lt Lung", "Lung_L", "Institution alias"),
            new StructureNameAlias("Left Lung", "Lung_L", "Institution alias"),
            new StructureNameAlias("L Lung", "Lung_L", "Institution alias"),
            new StructureNameAlias("Lung_Left", "Lung_L", "Institution alias"),
            new StructureNameAlias("LungL", "Lung_L", "Institution alias"),
            new StructureNameAlias("Both Lungs", "Lungs", "Institution alias"),
            new StructureNameAlias("Total Lung", "Lungs", "Institution alias"),
            new StructureNameAlias("Lung_Total", "Lungs", "Institution alias"),
            new StructureNameAlias("Esoph", "Esophagus", "Institution alias"),
            new StructureNameAlias("Oesophagus", "Esophagus", "Institution alias"),
            new StructureNameAlias("Trach", "Trachea", "Institution alias"),
            new StructureNameAlias("Rt Brachial Plexus", "BrachialPlex_R", "Institution alias"),
            new StructureNameAlias("Right Brachial Plexus", "BrachialPlex_R", "Institution alias"),
            new StructureNameAlias("Lt Brachial Plexus", "BrachialPlex_L", "Institution alias"),
            new StructureNameAlias("Left Brachial Plexus", "BrachialPlex_L", "Institution alias"),
            new StructureNameAlias("Brain Stem", "Brainstem", "Institution alias"),
            new StructureNameAlias("Rt Optic Nerve", "OpticNrv_R", "Institution alias"),
            new StructureNameAlias("Right Optic Nerve", "OpticNrv_R", "Institution alias"),
            new StructureNameAlias("Lt Optic Nerve", "OpticNrv_L", "Institution alias"),
            new StructureNameAlias("Left Optic Nerve", "OpticNrv_L", "Institution alias"),
            new StructureNameAlias("Chiasm", "OpticChiasm", "Institution alias"),
            new StructureNameAlias("Optic Chiasm", "OpticChiasm", "Institution alias"),
            new StructureNameAlias("Rt Eye", "Eye_R", "Institution alias"),
            new StructureNameAlias("Right Eye", "Eye_R", "Institution alias"),
            new StructureNameAlias("Lt Eye", "Eye_L", "Institution alias"),
            new StructureNameAlias("Left Eye", "Eye_L", "Institution alias"),
            new StructureNameAlias("Rt Lens", "Lens_R", "Institution alias"),
            new StructureNameAlias("Right Lens", "Lens_R", "Institution alias"),
            new StructureNameAlias("Lt Lens", "Lens_L", "Institution alias"),
            new StructureNameAlias("Left Lens", "Lens_L", "Institution alias"),
            new StructureNameAlias("Rt Cochlea", "Cochlea_R", "Institution alias"),
            new StructureNameAlias("Right Cochlea", "Cochlea_R", "Institution alias"),
            new StructureNameAlias("Lt Cochlea", "Cochlea_L", "Institution alias"),
            new StructureNameAlias("Left Cochlea", "Cochlea_L", "Institution alias"),
            new StructureNameAlias("Rt Parotid", "Parotid_R", "Institution alias"),
            new StructureNameAlias("Right Parotid", "Parotid_R", "Institution alias"),
            new StructureNameAlias("Lt Parotid", "Parotid_L", "Institution alias"),
            new StructureNameAlias("Left Parotid", "Parotid_L", "Institution alias"),
            new StructureNameAlias("Jaw", "Mandible", "Institution alias"),
            new StructureNameAlias("Oral Cavity", "OralCavity", "Institution alias"),
            new StructureNameAlias("Voice Box", "Larynx", "Institution alias"),
            new StructureNameAlias("Pharyngeal Constrictor", "Pharynx", "Institution alias"),
            new StructureNameAlias("Bladder Wall", "Bladder", "Institution alias"),
            new StructureNameAlias("Rectal Wall", "Rectum", "Institution alias"),
            new StructureNameAlias("Bowel Bag", "Bowel", "Institution alias"),
            new StructureNameAlias("Rt Femoral Head", "FemurHead_R", "Institution alias"),
            new StructureNameAlias("Right Femoral Head", "FemurHead_R", "Institution alias"),
            new StructureNameAlias("Lt Femoral Head", "FemurHead_L", "Institution alias"),
            new StructureNameAlias("Left Femoral Head", "FemurHead_L", "Institution alias")
        };

        var regexMappings = new[]
        {
            new StructureNameRegexMapping("^ptv[_ -]?70(00)?$", "PTV_7000", "Institution regex"),
            new StructureNameRegexMapping("^brain[_ -]?stem$", "Brainstem", "Institution regex"),
            new StructureNameRegexMapping("^gtv([_ -].*)?$", "GTV", "Institution regex"),
            new StructureNameRegexMapping("^ctv([_ -].*)?$", "CTV", "Institution regex")
        };

        var requiredStructures = new[]
        {
            "Body",
            "PTV_7000",
            "SpinalCord",
            "Heart",
            "Lung_R",
            "Lung_L"
        };

        return new StructureNameDictionary(
            "Synthetic TG-263 subset",
            canonicalNames,
            aliases,
            regexMappings,
            requiredStructures);
    }
}
