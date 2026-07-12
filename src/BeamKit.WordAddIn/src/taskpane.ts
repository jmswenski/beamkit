import "./taskpane.css";

type ExtractIssue = {
  severity?: string;
  code?: string;
  message?: string;
  section?: string;
  anchor?: string;
  path?: string;
};

type ExtractResponse = {
  id: string;
  isValid: boolean;
  wordErrorCount: number;
  wordWarningCount: number;
  validationErrorCount: number;
  validationWarningCount: number;
  sourceFileName: string;
  rtpxPackageBase64?: string;
  rtpxPackageFileName?: string;
  extraction?: {
    package?: ProtocolPackage;
    issues?: ExtractIssue[];
    validation?: {
      issues?: ExtractIssue[];
    };
  };
};

type PublishDraftResponse = {
  id: string;
  published: boolean;
  isValid: boolean;
  acceptance?: {
    id?: string;
    accepted?: boolean;
    rulePackId?: string;
    versionId?: string;
    reviewStatus?: string;
    packageFingerprint?: string;
    errorCount?: number;
    warningCount?: number;
  };
  rulePackImport?: {
    version?: {
      rulePackId?: string;
      versionId?: string;
      fingerprint?: string;
      isValid?: boolean;
      testPassed?: boolean;
    };
  };
  protocolDiff?: {
    isInitial?: boolean;
    changeCount?: number;
    changes?: ProtocolDiffChange[];
  };
  dashboardUrl?: string;
  extraction?: ExtractResponse["extraction"];
  wordErrorCount?: number;
  wordWarningCount?: number;
  validationErrorCount?: number;
  validationWarningCount?: number;
  sourceFileName?: string;
};

type ProtocolDiffChange = {
  category?: string;
  key?: string;
  changeType?: string;
  severity?: string;
  message?: string;
  before?: string;
  after?: string;
};

type TemplateLibraryResponse = {
  templates?: ProtocolTemplate[];
};

type SnippetLibraryResponse = {
  snippets?: ProtocolSnippet[];
};

type RowKind = "structures" | "prescriptions" | "constraints" | "checks";

type ProtocolPackage = {
  id?: string;
  name?: string;
  version?: string;
  diseaseSite?: string;
  intent?: string;
  status?: string;
  structures?: ProtocolStructure[];
  prescriptions?: ProtocolPrescription[];
  constraints?: ProtocolConstraint[];
  planChecks?: ProtocolPlanCheck[];
  workflow?: ProtocolWorkflow[];
};

type ProtocolStructure = {
  id?: string;
  name?: string;
  role?: string;
  level?: string;
};

type ProtocolPrescription = {
  id?: string;
  target?: string;
  totalDoseGy?: number | string;
  fractionCount?: number | string;
  fractions?: number | string;
  dosePerFractionGy?: number | string;
  technique?: string;
  energy?: string;
  level?: string;
};

type ProtocolConstraint = {
  id?: string;
  structure?: string;
  metric?: string;
  comparison?: string;
  value?: number | string;
  unit?: string;
  level?: string;
};

type ProtocolPlanCheck = {
  id?: string;
  title?: string;
  type?: string;
  level?: string;
};

type ProtocolWorkflow = {
  id?: string;
  title?: string;
  type?: string;
  level?: string;
};

type TemplateMetadata = {
  id: string;
  name: string;
  version: string;
  diseaseSite: string;
  intent: string;
  status: string;
  owner: string;
  tags: string;
  sourceTitle: string;
  sourceVersion: string;
};

type ProtocolTemplate = {
  key: string;
  label: string;
  metadata: TemplateMetadata;
  structures: readonly (readonly string[])[];
  prescriptions: readonly (readonly string[])[];
  constraints: readonly (readonly string[])[];
  checks: readonly (readonly string[])[];
  workflow: readonly (readonly string[])[];
};

type ProtocolSnippet = {
  key: string;
  label: string;
  table: RowKind;
  row: readonly string[];
};

type RtpxTableDefinition = {
  key: RowKind | "metadata" | "workflow";
  title: string;
  headers: readonly string[];
  sampleRows: readonly (readonly string[])[];
};

type RtpxTableMatch = {
  definition: RtpxTableDefinition;
  headerRowIndex: number;
};

const storageKeys = {
  serverUrl: "beamkit.wordAddIn.serverUrl",
  apiKey: "beamkit.wordAddIn.apiKey",
  includeSource: "beamkit.wordAddIn.includeSource"
};

const tableDefinitions: readonly RtpxTableDefinition[] = [
  {
    key: "metadata",
    title: "RT-PX Metadata",
    headers: ["Field", "Value"],
    sampleRows: [
      ["Id", "rtpx.example.protocol"],
      ["Name", "Example Protocol"],
      ["Version", "0.1.0"],
      ["Disease Site", "Example Site"],
      ["Intent", "Definitive"],
      ["Status", "Draft"],
      ["Reviewed By", ""],
      ["Approved By", ""],
      ["Effective Date", ""],
      ["Owner", "Protocol owner"],
      ["Tags", "example; word-source"],
      ["Source Title", "Source protocol document"],
      ["Source Version", "0.1.0"]
    ]
  },
  {
    key: "structures",
    title: "RT-PX Structures",
    headers: ["Id", "Name", "Role", "Level", "Aliases", "Must Have Contours", "Description"],
    sampleRows: [
      ["ptv", "PTV_5000", "Target", "Required", "PTV; Planning Target Volume", "yes", "Primary planning target"],
      ["cord", "Cord", "OAR", "Required", "SpinalCord", "yes", "Cord organ at risk"]
    ]
  },
  {
    key: "prescriptions",
    title: "RT-PX Prescriptions",
    headers: ["Id", "Target", "Total Dose Gy", "Fractions", "Dose Per Fraction Gy", "Technique", "Energy", "Level", "Description"],
    sampleRows: [
      ["rx.primary", "PTV_5000", "50", "5", "10", "VMAT", "6X", "Required", "Primary prescription"]
    ]
  },
  {
    key: "constraints",
    title: "RT-PX Dose Constraints",
    headers: ["Id", "Structure", "Metric", "Comparison", "Value", "Unit", "Level", "Description", "Active"],
    sampleRows: [
      ["cord.max", "Cord", "Max", "<=", "30", "Gy", "Required", "Cord max dose", "yes"]
    ]
  },
  {
    key: "checks",
    title: "RT-PX Plan Checks",
    headers: ["Id", "Title", "Type", "Level", "Parameters", "Description", "Active"],
    sampleRows: [
      ["dose-grid", "Dose grid <= 2.5 mm", "DoseGridResolution", "Required", "maxMm=2.5", "Protocol grid check", "yes"]
    ]
  },
  {
    key: "workflow",
    title: "RT-PX Workflow",
    headers: ["Id", "Title", "Type", "Level", "Description", "Active"],
    sampleRows: [
      ["physics.review", "Physics review before treatment", "Approval", "Required", "Protocol cases need physics review", "yes"]
    ]
  }
];

let protocolTemplates: ProtocolTemplate[] = [
  {
    key: "head-neck",
    label: "Head & Neck VMAT",
    metadata: {
      id: "rtpx.head-neck.vmat",
      name: "Head & Neck VMAT Protocol",
      version: "0.1.0",
      diseaseSite: "Head and Neck",
      intent: "Definitive",
      status: "Draft",
      owner: "Radiation Oncology",
      tags: "head-neck; vmat; authoring-template",
      sourceTitle: "Head & Neck protocol",
      sourceVersion: "draft"
    },
    structures: [
      ["ptv.high", "PTV_High", "Target", "Required", "PTV; PTV_HR", "yes", "High-risk planning target"],
      ["ctv.high", "CTV_High", "Target", "Recommended", "CTV; CTV_HR", "yes", "High-risk clinical target"],
      ["cord", "Cord", "OAR", "Required", "SpinalCord; Spinal Cord", "yes", "Spinal cord"],
      ["brainstem", "Brainstem", "OAR", "Required", "Brain Stem", "yes", "Brainstem"],
      ["parotid.l", "Parotid_L", "OAR", "Recommended", "Lt Parotid; Left Parotid", "yes", "Left parotid"],
      ["parotid.r", "Parotid_R", "OAR", "Recommended", "Rt Parotid; Right Parotid", "yes", "Right parotid"]
    ],
    prescriptions: [
      ["rx.high", "PTV_High", "70", "35", "2", "VMAT", "6X", "Required", "Primary definitive prescription"]
    ],
    constraints: [
      ["cord.max", "Cord", "Max", "<=", "45", "Gy", "Required", "Cord maximum dose", "yes"],
      ["brainstem.max", "Brainstem", "Max", "<=", "54", "Gy", "Required", "Brainstem maximum dose", "yes"],
      ["parotid.l.mean", "Parotid_L", "Mean", "<=", "26", "Gy", "Recommended", "Left parotid mean dose", "yes"],
      ["parotid.r.mean", "Parotid_R", "Mean", "<=", "26", "Gy", "Recommended", "Right parotid mean dose", "yes"],
      ["ptv.high.d95", "PTV_High", "D95", ">=", "95", "%", "Required", "High-risk target coverage", "yes"]
    ],
    checks: [
      ["dose-grid", "Dose grid <= 2.5 mm", "DoseGridResolution", "Required", "maxMm=2.5", "Protocol grid check", "yes"],
      ["beam-model", "Beam model matches policy", "BeamModel", "Required", "allowed=6X", "Beam model policy", "yes"],
      ["qa-plan-match", "QA plan matches treatment plan", "TreatmentQaPlanMatch", "Required", "", "QA plan integrity check", "yes"]
    ],
    workflow: [
      ["physics.review", "Physics review before treatment", "Approval", "Required", "Protocol cases need physics review", "yes"],
      ["peer.review", "Peer review before approval", "Review", "Recommended", "Review target and OAR tradeoffs", "yes"]
    ]
  },
  {
    key: "lung-sbrt",
    label: "Lung SBRT",
    metadata: {
      id: "rtpx.lung-sbrt",
      name: "Lung SBRT Protocol",
      version: "0.1.0",
      diseaseSite: "Lung",
      intent: "Definitive",
      status: "Draft",
      owner: "Radiation Oncology",
      tags: "lung; sbrt; authoring-template",
      sourceTitle: "Lung SBRT protocol",
      sourceVersion: "draft"
    },
    structures: [
      ["ptv", "PTV_5000", "Target", "Required", "PTV; PTV50", "yes", "SBRT planning target"],
      ["itv", "ITV", "Target", "Recommended", "Internal Target Volume", "yes", "Internal target volume"],
      ["lung.total", "Lung_Total", "OAR", "Required", "Total Lung; Lungs", "yes", "Total lung excluding target when institution policy requires"],
      ["cord", "Cord", "OAR", "Required", "SpinalCord; Spinal Cord", "yes", "Spinal cord"],
      ["heart", "Heart", "OAR", "Recommended", "", "yes", "Heart"],
      ["esophagus", "Esophagus", "OAR", "Recommended", "Oesophagus", "yes", "Esophagus"]
    ],
    prescriptions: [
      ["rx.primary", "PTV_5000", "50", "5", "10", "SBRT", "6X-FFF", "Required", "Common lung SBRT fractionation placeholder"]
    ],
    constraints: [
      ["ptv.d95", "PTV_5000", "D95", ">=", "95", "%", "Required", "PTV coverage", "yes"],
      ["lung.v20", "Lung_Total", "V20", "<=", "10", "%", "Recommended", "Total lung V20", "yes"],
      ["cord.max", "Cord", "Max", "<=", "30", "Gy", "Required", "Cord maximum dose", "yes"],
      ["heart.mean", "Heart", "Mean", "<=", "10", "Gy", "Recommended", "Mean heart dose", "yes"]
    ],
    checks: [
      ["dose-grid", "Dose grid <= 2.0 mm", "DoseGridResolution", "Required", "maxMm=2.0", "SBRT grid check", "yes"],
      ["mu-per-degree", "MU per degree >= minimum", "MuPerDegree", "Required", "minMuPerDegree=0.1", "Delivery modulation check", "yes"],
      ["qa-plan-match", "QA plan matches treatment plan", "TreatmentQaPlanMatch", "Required", "", "QA plan integrity check", "yes"]
    ],
    workflow: [
      ["physics.review", "Physics review before treatment", "Approval", "Required", "SBRT physics review", "yes"],
      ["image.guidance", "Image guidance documented", "Documentation", "Required", "Confirm SBRT setup and imaging expectations", "yes"]
    ]
  },
  {
    key: "prostate",
    label: "Prostate IMRT",
    metadata: {
      id: "rtpx.prostate.imrt",
      name: "Prostate IMRT Protocol",
      version: "0.1.0",
      diseaseSite: "Prostate",
      intent: "Definitive",
      status: "Draft",
      owner: "Radiation Oncology",
      tags: "prostate; imrt; authoring-template",
      sourceTitle: "Prostate protocol",
      sourceVersion: "draft"
    },
    structures: [
      ["ptv", "PTV_7000", "Target", "Required", "PTV", "yes", "Primary prostate planning target"],
      ["prostate", "Prostate", "Target", "Recommended", "", "yes", "Prostate gland"],
      ["rectum", "Rectum", "OAR", "Required", "", "yes", "Rectum"],
      ["bladder", "Bladder", "OAR", "Required", "", "yes", "Bladder"],
      ["femur.l", "Femur_Head_L", "OAR", "Recommended", "Left Femoral Head", "yes", "Left femoral head"],
      ["femur.r", "Femur_Head_R", "OAR", "Recommended", "Right Femoral Head", "yes", "Right femoral head"]
    ],
    prescriptions: [
      ["rx.primary", "PTV_7000", "70", "28", "2.5", "IMRT", "6X", "Required", "Moderate hypofractionation placeholder"]
    ],
    constraints: [
      ["ptv.d95", "PTV_7000", "D95", ">=", "95", "%", "Required", "PTV coverage", "yes"],
      ["rectum.v70", "Rectum", "V70", "<=", "15", "%", "Recommended", "Rectum high-dose volume", "yes"],
      ["bladder.v70", "Bladder", "V70", "<=", "25", "%", "Recommended", "Bladder high-dose volume", "yes"],
      ["femurs.max", "Femur_Head_L", "Max", "<=", "50", "Gy", "Recommended", "Femoral head max placeholder", "yes"]
    ],
    checks: [
      ["dose-grid", "Dose grid <= 2.5 mm", "DoseGridResolution", "Required", "maxMm=2.5", "Protocol grid check", "yes"],
      ["beam-model", "Beam model matches policy", "BeamModel", "Required", "allowed=6X;10X", "Beam model policy", "yes"]
    ],
    workflow: [
      ["physics.review", "Physics review before treatment", "Approval", "Required", "Physics review", "yes"]
    ]
  },
  {
    key: "breast",
    label: "Breast Tangents",
    metadata: {
      id: "rtpx.breast.tangents",
      name: "Breast Tangents Protocol",
      version: "0.1.0",
      diseaseSite: "Breast",
      intent: "Adjuvant",
      status: "Draft",
      owner: "Radiation Oncology",
      tags: "breast; tangents; authoring-template",
      sourceTitle: "Breast protocol",
      sourceVersion: "draft"
    },
    structures: [
      ["ptv.breast", "PTV_Breast", "Target", "Required", "Breast PTV", "yes", "Breast planning target"],
      ["heart", "Heart", "OAR", "Required", "", "yes", "Heart"],
      ["lung.ipsi", "Lung_Ipsi", "OAR", "Required", "Ipsilateral Lung", "yes", "Ipsilateral lung"],
      ["body", "Body", "External", "Required", "External", "yes", "External body"]
    ],
    prescriptions: [
      ["rx.primary", "PTV_Breast", "40.05", "15", "2.67", "3D", "6X", "Required", "Whole breast placeholder"]
    ],
    constraints: [
      ["ptv.d95", "PTV_Breast", "D95", ">=", "95", "%", "Required", "Target coverage", "yes"],
      ["heart.mean", "Heart", "Mean", "<=", "4", "Gy", "Recommended", "Mean heart dose placeholder", "yes"],
      ["lung.ipsi.v20", "Lung_Ipsi", "V20", "<=", "20", "%", "Recommended", "Ipsilateral lung V20", "yes"]
    ],
    checks: [
      ["dose-grid", "Dose grid <= 3.0 mm", "DoseGridResolution", "Required", "maxMm=3.0", "Protocol grid check", "yes"],
      ["qa-plan-match", "QA plan matches treatment plan", "TreatmentQaPlanMatch", "Required", "", "QA plan integrity check", "yes"]
    ],
    workflow: [
      ["physics.review", "Physics review before treatment", "Approval", "Required", "Physics review", "yes"]
    ]
  },
  {
    key: "brain-srs",
    label: "Brain SRS",
    metadata: {
      id: "rtpx.brain-srs",
      name: "Brain SRS Protocol",
      version: "0.1.0",
      diseaseSite: "Brain",
      intent: "Definitive",
      status: "Draft",
      owner: "Radiation Oncology",
      tags: "brain; srs; authoring-template",
      sourceTitle: "Brain SRS protocol",
      sourceVersion: "draft"
    },
    structures: [
      ["ptv", "PTV_2000", "Target", "Required", "PTV", "yes", "SRS planning target"],
      ["gtv", "GTV", "Target", "Recommended", "", "yes", "Gross target volume"],
      ["brainstem", "Brainstem", "OAR", "Required", "Brain Stem", "yes", "Brainstem"],
      ["optic.chiasm", "OpticChiasm", "OAR", "Recommended", "Chiasm", "yes", "Optic chiasm"],
      ["optic.nerve.l", "OpticNrv_L", "OAR", "Recommended", "Left Optic Nerve", "yes", "Left optic nerve"],
      ["optic.nerve.r", "OpticNrv_R", "OAR", "Recommended", "Right Optic Nerve", "yes", "Right optic nerve"]
    ],
    prescriptions: [
      ["rx.primary", "PTV_2000", "20", "1", "20", "SRS", "6X-FFF", "Required", "Single-fraction SRS placeholder"]
    ],
    constraints: [
      ["ptv.d95", "PTV_2000", "D95", ">=", "95", "%", "Required", "Target coverage", "yes"],
      ["brainstem.max", "Brainstem", "Max", "<=", "12.5", "Gy", "Required", "Brainstem max placeholder", "yes"],
      ["chiasm.max", "OpticChiasm", "Max", "<=", "8", "Gy", "Recommended", "Optic chiasm max placeholder", "yes"]
    ],
    checks: [
      ["dose-grid", "Dose grid <= 1.25 mm", "DoseGridResolution", "Required", "maxMm=1.25", "SRS grid check", "yes"],
      ["mu-per-degree", "MU per degree >= minimum", "MuPerDegree", "Required", "minMuPerDegree=0.1", "Delivery modulation check", "yes"],
      ["qa-plan-match", "QA plan matches treatment plan", "TreatmentQaPlanMatch", "Required", "", "QA plan integrity check", "yes"]
    ],
    workflow: [
      ["physics.review", "Physics review before treatment", "Approval", "Required", "SRS physics review", "yes"],
      ["peer.review", "Peer review before approval", "Review", "Required", "SRS peer review", "yes"]
    ]
  },
  {
    key: "palliative-bone",
    label: "Palliative Bone",
    metadata: {
      id: "rtpx.palliative-bone",
      name: "Palliative Bone Protocol",
      version: "0.1.0",
      diseaseSite: "Bone",
      intent: "Palliative",
      status: "Draft",
      owner: "Radiation Oncology",
      tags: "bone; palliative; authoring-template",
      sourceTitle: "Palliative bone protocol",
      sourceVersion: "draft"
    },
    structures: [
      ["ptv", "PTV_3000", "Target", "Required", "PTV", "yes", "Palliative planning target"],
      ["body", "Body", "External", "Required", "External", "yes", "External body"],
      ["cord", "Cord", "OAR", "Recommended", "SpinalCord; Spinal Cord", "yes", "Cord when near treatment field"]
    ],
    prescriptions: [
      ["rx.primary", "PTV_3000", "30", "10", "3", "3D", "6X", "Required", "Common palliative placeholder"]
    ],
    constraints: [
      ["ptv.d95", "PTV_3000", "D95", ">=", "95", "%", "Required", "Target coverage", "yes"],
      ["cord.max", "Cord", "Max", "<=", "30", "Gy", "Recommended", "Cord maximum dose if relevant", "yes"]
    ],
    checks: [
      ["dose-grid", "Dose grid <= 3.0 mm", "DoseGridResolution", "Required", "maxMm=3.0", "Protocol grid check", "yes"],
      ["qa-plan-match", "QA plan matches treatment plan", "TreatmentQaPlanMatch", "Required", "", "QA plan integrity check", "yes"]
    ],
    workflow: [
      ["physics.review", "Physics review before treatment", "Approval", "Required", "Physics review", "yes"]
    ]
  }
];

let protocolSnippets: ProtocolSnippet[] = [
  {
    key: "cord-max",
    label: "Cord max dose",
    table: "constraints",
    row: ["cord.max", "Cord", "Max", "<=", "45", "Gy", "Required", "Cord maximum dose", "yes"]
  },
  {
    key: "lung-v20",
    label: "Lung V20",
    table: "constraints",
    row: ["lung.v20", "Lung_Total", "V20", "<=", "35", "%", "Recommended", "Lung V20", "yes"]
  },
  {
    key: "heart-mean",
    label: "Mean heart dose",
    table: "constraints",
    row: ["heart.mean", "Heart", "Mean", "<=", "10", "Gy", "Recommended", "Mean heart dose", "yes"]
  },
  {
    key: "ptv-d95",
    label: "PTV D95 coverage",
    table: "constraints",
    row: ["ptv.d95", "PTV_5000", "D95", ">=", "95", "%", "Required", "PTV D95 coverage", "yes"]
  },
  {
    key: "dose-grid",
    label: "Dose grid check",
    table: "checks",
    row: ["dose-grid", "Dose grid <= 2.5 mm", "DoseGridResolution", "Required", "maxMm=2.5", "Protocol grid check", "yes"]
  },
  {
    key: "beam-model",
    label: "Beam model policy",
    table: "checks",
    row: ["beam-model", "Beam model matches policy", "BeamModel", "Required", "allowed=6X;10X", "Beam model policy", "yes"]
  },
  {
    key: "mu-per-degree",
    label: "MU per degree minimum",
    table: "checks",
    row: ["mu-per-degree", "MU per degree >= minimum", "MuPerDegree", "Required", "minMuPerDegree=0.1", "Delivery modulation check", "yes"]
  },
  {
    key: "qa-plan-match",
    label: "QA plan match",
    table: "checks",
    row: ["qa-plan-match", "QA plan matches treatment plan", "TreatmentQaPlanMatch", "Required", "", "QA plan integrity check", "yes"]
  }
];

let lastPackage: { base64: string; fileName: string } | null = null;
let lastDraftReviewUrl: string | null = null;
let lastIssues: ExtractIssue[] = [];

const insertScaffoldButton = element<HTMLButtonElement>("insertScaffoldButton");
const repairTablesButton = element<HTMLButtonElement>("repairTablesButton");
const refreshLibrariesButton = element<HTMLButtonElement>("refreshLibrariesButton");
const applyTemplateButton = element<HTMLButtonElement>("applyTemplateButton");
const templatePresetInput = element<HTMLSelectElement>("templatePreset");
const addSnippetButton = element<HTMLButtonElement>("addSnippetButton");
const snippetPresetInput = element<HTMLSelectElement>("snippetPreset");
const applyMetadataButton = element<HTMLButtonElement>("applyMetadataButton");
const addRowButton = element<HTMLButtonElement>("addRowButton");
const rowKindInput = element<HTMLSelectElement>("rowKind");
const rowFieldPanels: Record<RowKind, HTMLElement> = {
  structures: element<HTMLElement>("structureFields"),
  prescriptions: element<HTMLElement>("prescriptionFields"),
  constraints: element<HTMLElement>("constraintFields"),
  checks: element<HTMLElement>("checkFields")
};
const metadataIdInput = element<HTMLInputElement>("metadataId");
const metadataNameInput = element<HTMLInputElement>("metadataName");
const metadataVersionInput = element<HTMLInputElement>("metadataVersion");
const metadataDiseaseSiteInput = element<HTMLInputElement>("metadataDiseaseSite");
const metadataIntentInput = element<HTMLSelectElement>("metadataIntent");
const metadataStatusInput = element<HTMLSelectElement>("metadataStatus");
const metadataOwnerInput = element<HTMLInputElement>("metadataOwner");
const structureIdInput = element<HTMLInputElement>("structureId");
const structureNameInput = element<HTMLInputElement>("structureName");
const structureRoleInput = element<HTMLSelectElement>("structureRole");
const structureLevelInput = element<HTMLSelectElement>("structureLevel");
const structureAliasesInput = element<HTMLInputElement>("structureAliases");
const structureContoursInput = element<HTMLInputElement>("structureContours");
const structureDescriptionInput = element<HTMLInputElement>("structureDescription");
const prescriptionIdInput = element<HTMLInputElement>("prescriptionId");
const prescriptionTargetInput = element<HTMLInputElement>("prescriptionTarget");
const prescriptionTotalDoseInput = element<HTMLInputElement>("prescriptionTotalDose");
const prescriptionFractionsInput = element<HTMLInputElement>("prescriptionFractions");
const prescriptionTechniqueInput = element<HTMLInputElement>("prescriptionTechnique");
const prescriptionEnergyInput = element<HTMLInputElement>("prescriptionEnergy");
const prescriptionDescriptionInput = element<HTMLInputElement>("prescriptionDescription");
const constraintIdInput = element<HTMLInputElement>("constraintId");
const constraintStructureInput = element<HTMLInputElement>("constraintStructure");
const constraintMetricInput = element<HTMLSelectElement>("constraintMetric");
const constraintComparisonInput = element<HTMLSelectElement>("constraintComparison");
const constraintValueInput = element<HTMLInputElement>("constraintValue");
const constraintUnitInput = element<HTMLSelectElement>("constraintUnit");
const constraintDescriptionInput = element<HTMLInputElement>("constraintDescription");
const checkIdInput = element<HTMLInputElement>("checkId");
const checkTitleInput = element<HTMLInputElement>("checkTitle");
const checkTypeInput = element<HTMLSelectElement>("checkType");
const checkParametersInput = element<HTMLInputElement>("checkParameters");
const checkDescriptionInput = element<HTMLInputElement>("checkDescription");
const serverUrlInput = element<HTMLInputElement>("serverUrl");
const apiKeyInput = element<HTMLInputElement>("apiKey");
const includeSourceInput = element<HTMLInputElement>("includeSource");
const quickCheckButton = element<HTMLButtonElement>("quickCheckButton");
const extractButton = element<HTMLButtonElement>("extractButton");
const publishDraftButton = element<HTMLButtonElement>("publishDraftButton");
const openDraftButton = element<HTMLButtonElement>("openDraftButton");
const downloadButton = element<HTMLButtonElement>("downloadButton");
const statusText = element<HTMLElement>("statusText");
const statusBadge = element<HTMLElement>("statusBadge");
const summaryPanel = element<HTMLElement>("summaryPanel");
const protocolSummaryPanel = element<HTMLElement>("protocolSummaryPanel");
const protocolSummary = element<HTMLElement>("protocolSummary");
const issuesPanel = element<HTMLElement>("issuesPanel");
const protocolName = element<HTMLElement>("protocolName");
const packageName = element<HTMLElement>("packageName");
const issueCount = element<HTMLElement>("issueCount");
const draftStatus = element<HTMLElement>("draftStatus");
const issuesList = element<HTMLOListElement>("issuesList");

if (typeof Office === "undefined") {
  initialize();
} else {
  Office.onReady(() => initialize());
}

function initialize(): void {
  serverUrlInput.value = localStorage.getItem(storageKeys.serverUrl) ?? serverUrlInput.value;
  apiKeyInput.value = localStorage.getItem(storageKeys.apiKey) ?? "";
  includeSourceInput.checked = localStorage.getItem(storageKeys.includeSource) === "true";

  insertScaffoldButton.addEventListener("click", () => {
    void insertRtpxScaffold();
  });
  repairTablesButton.addEventListener("click", () => {
    void repairRtpxTables();
  });
  refreshLibrariesButton.addEventListener("click", () => {
    void loadAuthoringLibraries(true);
  });
  applyTemplateButton.addEventListener("click", () => {
    void applySelectedTemplate();
  });
  addSnippetButton.addEventListener("click", () => {
    void addSelectedSnippet();
  });
  applyMetadataButton.addEventListener("click", () => {
    void applyMetadata();
  });
  addRowButton.addEventListener("click", () => {
    void addSelectedRow();
  });
  rowKindInput.addEventListener("change", renderRowFields);
  quickCheckButton.addEventListener("click", () => {
    void quickCheckCurrentDocument();
  });
  extractButton.addEventListener("click", () => {
    void extractCurrentDocument();
  });
  publishDraftButton.addEventListener("click", () => {
    void publishCurrentDocumentDraft();
  });
  openDraftButton.addEventListener("click", openDraftReview);
  downloadButton.addEventListener("click", downloadPackage);

  renderAuthoringLibraryOptions();
  renderRowFields();
  setStatus("Ready to extract the active Word document.", "Idle", "idle");
  void loadAuthoringLibraries(false);
}

async function loadAuthoringLibraries(showStatus: boolean): Promise<void> {
  try {
    persistSettings();
    if (showStatus) {
      setStatus("Loading authoring libraries from BeamKit CI.", "Loading", "busy");
    }

    const [templateResponse, snippetResponse] = await Promise.all([
      fetch(`${normalizeServerUrl(serverUrlInput.value)}/api/rtpx/authoring/templates`, {
        headers: requestHeaders()
      }),
      fetch(`${normalizeServerUrl(serverUrlInput.value)}/api/rtpx/authoring/snippets`, {
        headers: requestHeaders()
      })
    ]);

    if (!templateResponse.ok) {
      throw new Error(await templateResponse.text() || `Template library returned ${templateResponse.status}.`);
    }

    if (!snippetResponse.ok) {
      throw new Error(await snippetResponse.text() || `Snippet library returned ${snippetResponse.status}.`);
    }

    const templateLibrary = await templateResponse.json() as TemplateLibraryResponse;
    const snippetLibrary = await snippetResponse.json() as SnippetLibraryResponse;
    if (templateLibrary.templates?.length) {
      protocolTemplates = templateLibrary.templates;
    }

    if (snippetLibrary.snippets?.length) {
      protocolSnippets = snippetLibrary.snippets;
    }

    renderAuthoringLibraryOptions();
    if (showStatus) {
      setStatus("Authoring libraries loaded.", "Done", "pass");
    }
  } catch (error) {
    renderAuthoringLibraryOptions();
    if (showStatus) {
      renderError(error);
      setStatus(error instanceof Error ? error.message : "Authoring library load failed.", "Failed", "fail");
    }
  }
}

function renderAuthoringLibraryOptions(): void {
  const selectedTemplateKey = templatePresetInput.value;
  templatePresetInput.replaceChildren(...protocolTemplates.map(template => {
    const option = document.createElement("option");
    option.value = template.key;
    option.textContent = template.label;
    return option;
  }));
  if (protocolTemplates.some(template => template.key === selectedTemplateKey)) {
    templatePresetInput.value = selectedTemplateKey;
  }

  const selectedSnippetKey = snippetPresetInput.value;
  snippetPresetInput.replaceChildren(...protocolSnippets.map(snippet => {
    const option = document.createElement("option");
    option.value = snippet.key;
    option.textContent = snippet.label;
    return option;
  }));
  if (protocolSnippets.some(snippet => snippet.key === selectedSnippetKey)) {
    snippetPresetInput.value = selectedSnippetKey;
  }
}

async function insertRtpxScaffold(): Promise<void> {
  await runWordEdit("Inserting RT-PX scaffold.", "Scaffold inserted.", async context => {
    const selection = context.document.getSelection();
    selection.insertHtml(createScaffoldHtml(), "End");
    await context.sync();
  });
}

async function repairRtpxTables(): Promise<void> {
  await runWordEdit("Repairing RT-PX tables.", "Tables repaired.", async context => {
    const tables = context.document.body.tables;
    tables.load("items/values,items/rowCount,items/isUniform");
    await context.sync();

    const foundKeys = new Set<RtpxTableDefinition["key"]>();
    for (const table of tables.items) {
      const match = resolveTableMatch(table.values);
      if (!match) {
        continue;
      }

      foundKeys.add(match.definition.key);
      if (!table.isUniform) {
        table.rows.getFirst().range.highlight();
        table.rows.getFirst().range.insertComment(`${match.definition.title} contains merged or non-uniform cells. Split merged cells before extraction.`);
        continue;
      }

      const repairedRows = repairTableRows(table.values, match);
      const rowDifference = repairedRows.length - table.rowCount;
      if (rowDifference > 0) {
        table.addRows("End", rowDifference, Array.from({ length: rowDifference }, () => emptyRow(match.definition)));
        await context.sync();
      }

      table.values = repairedRows;
    }

    const missingTables = tableDefinitions.filter(definition => !foundKeys.has(definition.key));
    if (missingTables.length > 0) {
      context.document.body.insertHtml(createTablesHtml(missingTables), "End");
    }

    await context.sync();
  });
}

async function applySelectedTemplate(): Promise<void> {
  const template = selectedTemplate();
  applyTemplateMetadataToFields(template.metadata);
  await runWordEdit(`Inserting ${template.label} template.`, "Template inserted.", async context => {
    const selection = context.document.getSelection();
    selection.insertHtml(createTemplateHtml(template), "End");
    await context.sync();
  });
}

async function addSelectedSnippet(): Promise<void> {
  const snippet = selectedSnippet();
  rowKindInput.value = snippet.table;
  renderRowFields();

  await runWordEdit(`Adding ${snippet.label}.`, "Snippet added.", async context => {
    const table = await findRequiredRtpxTable(context, snippet.table);
    table.addRows("End", 1, [Array.from(snippet.row)]);
    await context.sync();
  });
}

async function applyMetadata(): Promise<void> {
  await runWordEdit("Applying metadata.", "Metadata applied.", async context => {
    const table = await findRequiredRtpxTable(context, "metadata");
    const nextRows = createMetadataRows(table.values);
    const missingRows = nextRows.length - table.rowCount;
    if (missingRows > 0) {
      table.addRows("End", missingRows, Array.from({ length: missingRows }, () => ["", ""]));
      await context.sync();
    }

    table.values = nextRows;
    await context.sync();
  });
}

async function addSelectedRow(): Promise<void> {
  const rowKind = selectedRowKind();
  await runWordEdit("Adding RT-PX row.", "Row added.", async context => {
    const table = await findRequiredRtpxTable(context, rowKind);
    table.addRows("End", 1, [createRow(rowKind)]);
    await context.sync();
  });
}

async function runWordEdit(
  busyMessage: string,
  successMessage: string,
  edit: (context: Word.RequestContext) => Promise<void>): Promise<void> {
  try {
    ensureWordHost();
    setBusy(true);
    setStatus(busyMessage, "Editing", "busy");
    await Word.run(async context => {
      await edit(context);
    });
    setStatus(successMessage, "Done", "pass");
  } catch (error) {
    renderError(error);
    setStatus(error instanceof Error ? error.message : "Word edit failed.", "Failed", "fail");
  } finally {
    setBusy(false);
  }
}

function renderRowFields(): void {
  const selected = selectedRowKind();
  for (const [kind, panel] of Object.entries(rowFieldPanels) as Array<[RowKind, HTMLElement]>) {
    panel.hidden = kind !== selected;
  }
}

async function findRequiredRtpxTable(
  context: Word.RequestContext,
  key: RtpxTableDefinition["key"]): Promise<Word.Table> {
  const definition = tableDefinition(key);
  const tables = context.document.body.tables;
  tables.load("items/values,items/rowCount");
  await context.sync();

  const table = tables.items.find(item => resolveTableMatch(item.values)?.definition.key === definition.key);
  if (!table) {
    throw new Error(`${definition.title} table was not found. Insert Scaffold first.`);
  }

  return table;
}

function rowMatchesHeaders(row: string[], headers: readonly string[]): boolean {
  if (row.length < headers.length) {
    return false;
  }

  return headers.every((header, index) => normalizeCell(row[index]) === normalizeCell(header));
}

function resolveTableMatch(values: string[][]): RtpxTableMatch | null {
  for (const definition of tableDefinitions) {
    const titleRowIndex = values.findIndex(row => row.some(cell => normalizeCell(cell) === normalizeCell(definition.title)));
    if (titleRowIndex >= 0 && rowMatchesHeaders(values[titleRowIndex + 1] ?? [], definition.headers)) {
      return { definition, headerRowIndex: titleRowIndex + 1 };
    }

    const headerRowIndex = values.findIndex(row => rowMatchesHeaders(row, definition.headers));
    if (headerRowIndex >= 0) {
      return { definition, headerRowIndex };
    }
  }

  return null;
}

function repairTableRows(values: string[][], match: RtpxTableMatch): string[][] {
  const rows = [Array.from(match.definition.headers)];
  for (const row of values.slice(match.headerRowIndex + 1)) {
    const normalized = normalizeTableRow(row, match.definition);
    if (normalized.some(cell => cell.trim().length > 0)) {
      rows.push(normalized);
    }
  }

  if (rows.length === 1) {
    rows.push(...match.definition.sampleRows.map(row => Array.from(row)));
  }

  while (rows.length < values.length) {
    rows.push(emptyRow(match.definition));
  }

  return rows;
}

function normalizeTableRow(row: string[], definition: RtpxTableDefinition): string[] {
  return Array.from({ length: definition.headers.length }, (_, index) => row[index]?.trim() ?? "");
}

function emptyRow(definition: RtpxTableDefinition): string[] {
  return Array.from({ length: definition.headers.length }, () => "");
}

function createMetadataRows(existingValues: string[][]): string[][] {
  const values = new Map<string, string>();
  for (const row of existingValues.slice(1)) {
    const key = row[0]?.trim();
    if (key) {
      values.set(key, row[1] ?? "");
    }
  }

  const updates: Array<[string, string]> = [
    ["Id", requiredValue(metadataIdInput, "Protocol id")],
    ["Name", requiredValue(metadataNameInput, "Protocol name")],
    ["Version", requiredValue(metadataVersionInput, "Protocol version")],
    ["Disease Site", requiredValue(metadataDiseaseSiteInput, "Disease site")],
    ["Intent", metadataIntentInput.value],
    ["Status", metadataStatusInput.value],
    ["Owner", requiredValue(metadataOwnerInput, "Owner")]
  ];

  for (const [key, value] of updates) {
    values.set(key, value);
  }

  const orderedKeys = [
    "Id",
    "Name",
    "Version",
    "Disease Site",
    "Intent",
    "Status",
    "Reviewed By",
    "Approved By",
    "Effective Date",
    "Owner",
    "Tags",
    "Source Title",
    "Source Version"
  ];

  const rows = [["Field", "Value"]];
  for (const key of orderedKeys) {
    rows.push([key, values.get(key) ?? ""]);
  }

  for (const [key, value] of values) {
    if (!orderedKeys.includes(key)) {
      rows.push([key, value]);
    }
  }

  return rows;
}

function createRow(kind: RowKind): string[] {
  switch (kind) {
    case "structures":
      return [
        requiredValue(structureIdInput, "Structure id"),
        requiredValue(structureNameInput, "Structure name"),
        structureRoleInput.value,
        structureLevelInput.value,
        structureAliasesInput.value.trim(),
        structureContoursInput.checked ? "yes" : "no",
        structureDescriptionInput.value.trim()
      ];
    case "prescriptions": {
      const totalDose = requiredValue(prescriptionTotalDoseInput, "Total dose");
      const fractions = requiredValue(prescriptionFractionsInput, "Fractions");
      return [
        requiredValue(prescriptionIdInput, "Prescription id"),
        requiredValue(prescriptionTargetInput, "Prescription target"),
        totalDose,
        fractions,
        calculateDosePerFraction(totalDose, fractions),
        requiredValue(prescriptionTechniqueInput, "Technique"),
        requiredValue(prescriptionEnergyInput, "Energy"),
        "Required",
        prescriptionDescriptionInput.value.trim()
      ];
    }
    case "constraints":
      return [
        requiredValue(constraintIdInput, "Constraint id"),
        requiredValue(constraintStructureInput, "Constraint structure"),
        constraintMetricInput.value,
        constraintComparisonInput.value,
        requiredValue(constraintValueInput, "Constraint value"),
        constraintUnitInput.value,
        "Required",
        constraintDescriptionInput.value.trim(),
        "yes"
      ];
    case "checks":
      return [
        requiredValue(checkIdInput, "Plan check id"),
        requiredValue(checkTitleInput, "Plan check title"),
        checkTypeInput.value,
        "Required",
        checkParametersInput.value.trim(),
        checkDescriptionInput.value.trim(),
        "yes"
      ];
  }
}

function createScaffoldHtml(): string {
  return [
    "<h1>RT-PX Protocol Template</h1>",
    "<p>Replace sample values, then validate with BeamKit.</p>",
    createTablesHtml(tableDefinitions)
  ].join("");
}

function createTemplateHtml(template: ProtocolTemplate): string {
  const rowsByKey = new Map<RtpxTableDefinition["key"], readonly (readonly string[])[]>([
    ["metadata", createTemplateMetadataRows(template.metadata)],
    ["structures", template.structures],
    ["prescriptions", template.prescriptions],
    ["constraints", template.constraints],
    ["checks", template.checks],
    ["workflow", template.workflow]
  ]);

  return [
    `<h1>${escapeHtml(template.label)} RT-PX Template</h1>`,
    "<p>Review every value against the source protocol and institution policy before clinical use.</p>",
    tableDefinitions.map(definition => createTableHtml(definition, rowsByKey.get(definition.key) ?? definition.sampleRows)).join("")
  ].join("");
}

function createTablesHtml(definitions: readonly RtpxTableDefinition[]): string {
  return definitions.map(definition => createTableHtml(definition, definition.sampleRows)).join("");
}

function createTableHtml(definition: RtpxTableDefinition, dataRows: readonly (readonly string[])[]): string {
  const rows = [definition.headers, ...dataRows]
    .map(row => `<tr>${row.map(cell => `<td>${escapeHtml(cell)}</td>`).join("")}</tr>`)
    .join("");
  return `<h2>${escapeHtml(definition.title)}</h2><table border="1" cellspacing="0" cellpadding="4">${rows}</table>`;
}

function createTemplateMetadataRows(metadata: TemplateMetadata): readonly (readonly string[])[] {
  return [
    ["Id", metadata.id],
    ["Name", metadata.name],
    ["Version", metadata.version],
    ["Disease Site", metadata.diseaseSite],
    ["Intent", metadata.intent],
    ["Status", metadata.status],
    ["Reviewed By", ""],
    ["Approved By", ""],
    ["Effective Date", ""],
    ["Owner", metadata.owner],
    ["Tags", metadata.tags],
    ["Source Title", metadata.sourceTitle],
    ["Source Version", metadata.sourceVersion]
  ];
}

function tableDefinition(key: RtpxTableDefinition["key"]): RtpxTableDefinition {
  const definition = tableDefinitions.find(item => item.key === key);
  if (!definition) {
    throw new Error(`Unsupported RT-PX table '${key}'.`);
  }

  return definition;
}

function selectedRowKind(): RowKind {
  const value = rowKindInput.value;
  if (value === "structures" || value === "prescriptions" || value === "constraints" || value === "checks") {
    return value;
  }

  throw new Error(`Unsupported row kind '${value}'.`);
}

function selectedTemplate(): ProtocolTemplate {
  const template = protocolTemplates.find(item => item.key === templatePresetInput.value);
  if (!template) {
    throw new Error(`Unsupported protocol template '${templatePresetInput.value}'.`);
  }

  return template;
}

function selectedSnippet(): ProtocolSnippet {
  const snippet = protocolSnippets.find(item => item.key === snippetPresetInput.value);
  if (!snippet) {
    throw new Error(`Unsupported protocol snippet '${snippetPresetInput.value}'.`);
  }

  return snippet;
}

function applyTemplateMetadataToFields(metadata: TemplateMetadata): void {
  metadataIdInput.value = metadata.id;
  metadataNameInput.value = metadata.name;
  metadataVersionInput.value = metadata.version;
  metadataDiseaseSiteInput.value = metadata.diseaseSite;
  metadataIntentInput.value = metadata.intent;
  metadataStatusInput.value = metadata.status;
  metadataOwnerInput.value = metadata.owner;
}

function requiredValue(input: HTMLInputElement, label: string): string {
  const value = input.value.trim();
  if (value.length === 0) {
    throw new Error(`${label} is required.`);
  }

  return value;
}

function calculateDosePerFraction(totalDoseText: string, fractionsText: string): string {
  const totalDose = Number(totalDoseText);
  const fractions = Number(fractionsText);
  if (!Number.isFinite(totalDose) || !Number.isFinite(fractions) || fractions <= 0) {
    throw new Error("Total dose and fractions must be numeric.");
  }

  return trimDecimal(totalDose / fractions);
}

function trimDecimal(value: number): string {
  return value.toFixed(4).replace(/\.?0+$/, "");
}

function normalizeCell(value: string): string {
  return value.trim().replace(/\s+/g, " ").toLowerCase();
}

function escapeHtml(value: string): string {
  return value
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;");
}

function ensureWordHost(): void {
  if (typeof Office === "undefined" || typeof Word === "undefined") {
    throw new Error("Open this task pane inside Microsoft Word to edit the active document.");
  }
}

async function quickCheckCurrentDocument(): Promise<void> {
  await checkCurrentDocument(false);
}

async function extractCurrentDocument(): Promise<void> {
  await checkCurrentDocument(true);
}

async function publishCurrentDocumentDraft(): Promise<void> {
  try {
    setBusy(true);
    setStatus("Reading the active Word document.", "Reading", "busy");
    persistSettings();

    const docxBase64 = await readCurrentDocumentBase64();
    setStatus("Publishing draft to BeamKit CI.", "Publishing", "busy");

    const response = await fetch(`${normalizeServerUrl(serverUrlInput.value)}/api/rtpx/word/publish-draft`, {
      method: "POST",
      headers: requestHeaders(),
      body: JSON.stringify({
        fileName: currentDocumentFileName(),
        docxBase64,
        includeSourceDocument: includeSourceInput.checked,
        importedBy: "BeamKit.WordAddIn",
        runRegressionTests: true,
        clientContext: {
          caller: "BeamKit.WordAddIn",
          mode: "publish-draft",
          officeHost: typeof Office === "undefined" ? "browser" : String(Office.context.host)
        }
      })
    });

    const body = await response.text();
    if (!response.ok) {
      throw new Error(body || `BeamKit CI returned ${response.status}.`);
    }

    const result = JSON.parse(body) as PublishDraftResponse;
    renderDraftPublishResult(result);
    setStatus(
      result.published ? "Draft published for review." : "Draft publish needs review.",
      result.published ? "Draft" : "Review",
      result.published ? "pass" : "fail");
  } catch (error) {
    lastPackage = null;
    lastDraftReviewUrl = null;
    downloadButton.disabled = true;
    openDraftButton.disabled = true;
    renderError(error);
    setStatus(error instanceof Error ? error.message : "Draft publish failed.", "Failed", "fail");
  } finally {
    setBusy(false);
  }
}

async function checkCurrentDocument(generatePackage: boolean): Promise<void> {
  try {
    setBusy(true);
    setStatus("Reading the active Word document.", "Reading", "busy");
    persistSettings();

    const docxBase64 = await readCurrentDocumentBase64();
    setStatus("Posting document to BeamKit CI.", "Uploading", "busy");

    const response = await fetch(`${normalizeServerUrl(serverUrlInput.value)}/api/rtpx/word/extract`, {
      method: "POST",
      headers: requestHeaders(),
      body: JSON.stringify({
        fileName: currentDocumentFileName(),
        docxBase64,
        includeSourceDocument: includeSourceInput.checked,
        generatePackage,
        clientContext: {
          caller: "BeamKit.WordAddIn",
          mode: generatePackage ? "extract" : "quick-check",
          officeHost: typeof Office === "undefined" ? "browser" : String(Office.context.host)
        }
      })
    });

    const body = await response.text();
    if (!response.ok) {
      throw new Error(body || `BeamKit CI returned ${response.status}.`);
    }

    const result = JSON.parse(body) as ExtractResponse;
    renderResult(result);
    setStatus(
      result.isValid
        ? generatePackage ? "RT-PX extraction passed." : "Quick check passed."
        : generatePackage ? "RT-PX extraction needs review." : "Quick check needs review.",
      result.isValid ? "Pass" : "Review",
      result.isValid ? "pass" : "fail");
  } catch (error) {
    lastPackage = null;
    lastDraftReviewUrl = null;
    downloadButton.disabled = true;
    openDraftButton.disabled = true;
    renderError(error);
    setStatus(error instanceof Error ? error.message : "RT-PX extraction failed.", "Failed", "fail");
  } finally {
    setBusy(false);
  }
}

function requestHeaders(): HeadersInit {
  const headers: Record<string, string> = {
    "content-type": "application/json"
  };
  const apiKey = apiKeyInput.value.trim();
  if (apiKey.length > 0) {
    headers["X-BeamKit-Api-Key"] = apiKey;
  }

  return headers;
}

function renderResult(result: ExtractResponse): void {
  lastDraftReviewUrl = null;
  openDraftButton.disabled = true;
  const protocol = result.extraction?.package;
  const issues = [
    ...(result.extraction?.issues ?? []),
    ...(result.extraction?.validation?.issues ?? [])
  ];
  lastIssues = issues;

  protocolName.textContent = protocol?.name
    ? `${protocol.name} (${protocol.id ?? "no id"})`
    : result.sourceFileName;
  packageName.textContent = result.rtpxPackageFileName ?? "Not generated";
  issueCount.textContent = [
    `${result.wordErrorCount + result.validationErrorCount} errors`,
    `${result.wordWarningCount + result.validationWarningCount} warnings`
  ].join(", ");
  draftStatus.textContent = "-";

  summaryPanel.hidden = false;
  renderProtocolSummary(protocol);
  renderIssues(issues);

  if (result.rtpxPackageBase64 && result.rtpxPackageFileName) {
    lastPackage = {
      base64: result.rtpxPackageBase64,
      fileName: result.rtpxPackageFileName
    };
    downloadButton.disabled = false;
  } else {
    lastPackage = null;
    downloadButton.disabled = true;
  }
}

function renderDraftPublishResult(result: PublishDraftResponse): void {
  const protocol = result.extraction?.package;
  const extractionIssues = [
    ...(result.extraction?.issues ?? []),
    ...(result.extraction?.validation?.issues ?? [])
  ];
  const diffIssues = (result.protocolDiff?.changes ?? []).map(change => ({
    severity: change.severity ?? "Review",
    code: `rtpx.diff.${change.changeType ?? "change"}`,
    message: change.message ?? "Protocol draft changed.",
    section: change.category,
    anchor: change.key,
    path: [change.before ? `before: ${change.before}` : null, change.after ? `after: ${change.after}` : null]
      .filter(Boolean)
      .join(" | ")
  } satisfies ExtractIssue));
  const issues = [...extractionIssues, ...diffIssues];
  lastIssues = issues;
  lastDraftReviewUrl = result.dashboardUrl
    ? `${normalizeServerUrl(serverUrlInput.value)}${result.dashboardUrl}`
    : null;
  openDraftButton.disabled = !lastDraftReviewUrl;

  protocolName.textContent = protocol?.name
    ? `${protocol.name} (${protocol.id ?? "no id"})`
    : result.sourceFileName ?? "Word protocol";
  packageName.textContent = result.acceptance?.packageFingerprint
    ? `${result.acceptance.packageFingerprint.slice(0, 18)}...`
    : "Not accepted";
  const errorCount = (result.wordErrorCount ?? 0) + (result.validationErrorCount ?? 0);
  const warningCount = (result.wordWarningCount ?? 0) + (result.validationWarningCount ?? 0);
  issueCount.textContent = [
    `${errorCount} errors`,
    `${warningCount} warnings`,
    `${result.protocolDiff?.changeCount ?? 0} changes`
  ].join(", ");
  draftStatus.textContent = result.acceptance?.id
    ? `${result.acceptance.reviewStatus ?? (result.acceptance.accepted ? "Draft" : "Rejected")} | ${result.acceptance.id} | ${result.acceptance.rulePackId ?? "rule pack pending"} | ${result.acceptance.versionId ?? "version pending"}`
    : "Not published";

  summaryPanel.hidden = false;
  renderProtocolSummary(protocol);
  renderIssues(issues);
  lastPackage = null;
  downloadButton.disabled = true;
}

function renderProtocolSummary(protocol: ProtocolPackage | undefined): void {
  protocolSummary.replaceChildren();
  protocolSummaryPanel.hidden = !protocol;
  if (!protocol) {
    return;
  }

  const counts = document.createElement("p");
  counts.className = "summary-counts";
  counts.textContent = [
    `${protocol.structures?.length ?? 0} structures`,
    `${protocol.prescriptions?.length ?? 0} prescriptions`,
    `${protocol.constraints?.length ?? 0} constraints`,
    `${protocol.planChecks?.length ?? 0} plan checks`,
    `${protocol.workflow?.length ?? 0} workflow items`
  ].join(" | ");
  protocolSummary.append(counts);

  appendSummarySection(protocolSummary, "Structures", protocol.structures ?? [], structureSummary);
  appendSummarySection(protocolSummary, "Prescriptions", protocol.prescriptions ?? [], prescriptionSummary);
  appendSummarySection(protocolSummary, "Dose Constraints", protocol.constraints ?? [], constraintSummary);
  appendSummarySection(protocolSummary, "Plan Checks", protocol.planChecks ?? [], planCheckSummary);
  appendSummarySection(protocolSummary, "Workflow", protocol.workflow ?? [], workflowSummary);
}

function appendSummarySection<T>(
  parent: HTMLElement,
  titleText: string,
  items: readonly T[],
  formatItem: (item: T) => string): void {
  const section = document.createElement("section");
  section.className = "summary-block";
  const title = document.createElement("h3");
  title.textContent = titleText;
  section.append(title);

  if (items.length === 0) {
    const empty = document.createElement("p");
    empty.textContent = "None captured.";
    section.append(empty);
    parent.append(section);
    return;
  }

  const list = document.createElement("ul");
  for (const item of items) {
    const row = document.createElement("li");
    row.textContent = formatItem(item);
    list.append(row);
  }

  section.append(list);
  parent.append(section);
}

function structureSummary(item: ProtocolStructure): string {
  return [item.name ?? item.id ?? "Unnamed structure", item.role, item.level].filter(Boolean).join(" | ");
}

function prescriptionSummary(item: ProtocolPrescription): string {
  const fractions = item.fractionCount ?? item.fractions;
  const doseText = item.totalDoseGy && fractions
    ? `${item.totalDoseGy} Gy in ${fractions} fx`
    : item.totalDoseGy ? `${item.totalDoseGy} Gy` : "dose not specified";
  const perFraction = item.dosePerFractionGy ? ` (${item.dosePerFractionGy} Gy/fx)` : "";
  return [
    `${item.target ?? item.id ?? "Unnamed target"}: ${doseText}${perFraction}`,
    item.technique,
    item.energy,
    item.level
  ].filter(Boolean).join(" | ");
}

function constraintSummary(item: ProtocolConstraint): string {
  return [
    `${item.structure ?? "Structure"} ${item.metric ?? "metric"} ${item.comparison ?? "?"} ${item.value ?? "value"} ${item.unit ?? ""}`.trim(),
    item.level
  ].filter(Boolean).join(" | ");
}

function planCheckSummary(item: ProtocolPlanCheck): string {
  return [item.title ?? item.id ?? "Unnamed plan check", item.type, item.level].filter(Boolean).join(" | ");
}

function workflowSummary(item: ProtocolWorkflow): string {
  return [item.title ?? item.id ?? "Unnamed workflow item", item.type, item.level].filter(Boolean).join(" | ");
}

function renderIssues(issues: ExtractIssue[]): void {
  issuesList.replaceChildren();
  issuesPanel.hidden = issues.length === 0;

  for (const [index, issue] of issues.entries()) {
    const item = document.createElement("li");
    const title = document.createElement("strong");
    title.textContent = `${issue.severity ?? "Issue"}${issue.code ? ` ${issue.code}` : ""}`;
    const message = document.createElement("span");
    message.textContent = ` ${issue.message ?? "No message supplied."}`;
    const meta = document.createElement("span");
    meta.className = "issue-meta";
    meta.textContent = [issue.section, issue.anchor, issue.path].filter(Boolean).join(" | ");
    item.append(title, message);
    if (meta.textContent.length > 0) {
      item.append(meta);
    }

    const actions = createIssueActions(issue, index);
    if (actions) {
      item.append(actions);
    }

    issuesList.append(item);
  }
}

function renderError(error: unknown): void {
  lastIssues = [];
  lastDraftReviewUrl = null;
  summaryPanel.hidden = true;
  draftStatus.textContent = "-";
  openDraftButton.disabled = true;
  protocolSummaryPanel.hidden = true;
  protocolSummary.replaceChildren();
  issuesPanel.hidden = false;
  issuesList.replaceChildren();
  const item = document.createElement("li");
  item.textContent = error instanceof Error ? error.message : "Unknown extraction error.";
  issuesList.append(item);
}

function createIssueActions(issue: ExtractIssue, index: number): HTMLElement | null {
  const canLocate = canLocateIssue(issue);
  const fixLabel = fixLabelForIssue(issue);
  if (!canLocate && !fixLabel) {
    return null;
  }

  const actions = document.createElement("div");
  actions.className = "issue-actions";

  if (canLocate) {
    actions.append(createIssueButton("Go", () => {
      void navigateToIssue(index);
    }));
    actions.append(createIssueButton("Comment", () => {
      void commentOnIssue(index);
    }));
  }

  if (fixLabel) {
    actions.append(createIssueButton(fixLabel, () => {
      void fixIssue(index);
    }));
  }

  return actions;
}

function createIssueButton(label: string, onClick: () => void): HTMLButtonElement {
  const button = document.createElement("button");
  button.type = "button";
  button.className = "issue-action";
  button.textContent = label;
  button.addEventListener("click", onClick);
  return button;
}

async function navigateToIssue(index: number): Promise<void> {
  const issue = lastIssues[index];
  if (!issue) {
    return;
  }

  await runWordEdit("Finding issue row.", "Issue row selected.", async context => {
    const row = await findIssueRow(context, issue);
    row.select();
    row.range.highlight();
    await context.sync();
  });
}

async function commentOnIssue(index: number): Promise<void> {
  const issue = lastIssues[index];
  if (!issue) {
    return;
  }

  await runWordEdit("Adding issue comment.", "Comment added.", async context => {
    const row = await findIssueRow(context, issue);
    row.range.insertComment(issue.message ?? "BeamKit RT-PX issue.");
    row.select();
    await context.sync();
  });
}

async function fixIssue(index: number): Promise<void> {
  const issue = lastIssues[index];
  if (!issue) {
    return;
  }

  const fixLabel = fixLabelForIssue(issue);
  if (fixLabel === "Apply Metadata") {
    await applyMetadata();
    return;
  }

  await repairRtpxTables();
}

async function findIssueRow(context: Word.RequestContext, issue: ExtractIssue): Promise<Word.TableRow> {
  const tables = context.document.body.tables;
  tables.load("items/values,items/rowCount,items/rows/items");
  await context.sync();

  const location = issueLocation(issue);
  let table: Word.Table | undefined;
  if (location.tableNumber !== null) {
    table = tables.items[location.tableNumber - 1];
  }

  if (!table) {
    const definition = definitionForIssue(issue);
    table = definition
      ? tables.items.find(item => resolveTableMatch(item.values)?.definition.key === definition.key)
      : undefined;
  }

  if (!table) {
    throw new Error("Could not find the table referenced by this issue.");
  }

  const rowIndex = Math.min(Math.max((location.rowNumber ?? 1) - 1, 0), table.rows.items.length - 1);
  const row = table.rows.items[rowIndex];
  if (!row) {
    throw new Error("Could not find the row referenced by this issue.");
  }

  return row;
}

function canLocateIssue(issue: ExtractIssue): boolean {
  return issueLocation(issue).tableNumber !== null || definitionForIssue(issue) !== null;
}

function fixLabelForIssue(issue: ExtractIssue): string | null {
  const text = issueText(issue);
  if (text.includes("merged")) {
    return null;
  }

  if (text.includes("metadata") || text.includes("disease site") || text.includes("intent") || text.includes("protocol id")) {
    return "Apply Metadata";
  }

  if (text.includes("header") || text.includes("table") || text.includes("row shape") || text.includes("column")) {
    return "Repair Tables";
  }

  return null;
}

function definitionForIssue(issue: ExtractIssue): RtpxTableDefinition | null {
  const text = issueText(issue);
  return tableDefinitions.find(definition =>
    text.includes(normalizeCell(definition.title))
    || text.includes(normalizeCell(definition.title.replace("RT-PX ", "")))) ?? null;
}

function issueLocation(issue: ExtractIssue): { tableNumber: number | null; rowNumber: number | null } {
  const text = [issue.anchor, issue.message].filter(Boolean).join(" ");
  const tableMatch = /\btable\s+(\d+)/i.exec(text);
  const rowMatch = /\brow\s+(\d+)/i.exec(text);
  return {
    tableNumber: tableMatch ? Number(tableMatch[1]) : null,
    rowNumber: rowMatch ? Number(rowMatch[1]) : null
  };
}

function issueText(issue: ExtractIssue): string {
  return normalizeCell([
    issue.section,
    issue.anchor,
    issue.path,
    issue.code,
    issue.message
  ].filter(Boolean).join(" "));
}

function downloadPackage(): void {
  if (!lastPackage) {
    return;
  }

  const bytes = base64ToBytes(lastPackage.base64);
  const arrayBuffer = new ArrayBuffer(bytes.byteLength);
  new Uint8Array(arrayBuffer).set(bytes);
  const blob = new Blob([arrayBuffer], { type: "application/zip" });
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement("a");
  anchor.href = url;
  anchor.download = lastPackage.fileName;
  anchor.click();
  URL.revokeObjectURL(url);
}

function openDraftReview(): void {
  if (!lastDraftReviewUrl) {
    return;
  }

  window.open(lastDraftReviewUrl, "_blank", "noopener,noreferrer");
}

function readCurrentDocumentBase64(): Promise<string> {
  if (typeof Office === "undefined") {
    return Promise.reject(new Error("Open this task pane inside Microsoft Word to read the active document."));
  }

  return new Promise((resolve, reject) => {
    Office.context.document.getFileAsync(
      Office.FileType.Compressed,
      { sliceSize: 1024 * 1024 },
      fileResult => {
        if (fileResult.status !== Office.AsyncResultStatus.Succeeded) {
          reject(new Error(fileResult.error.message));
          return;
        }

        const file = fileResult.value;
        const chunks: Uint8Array[] = [];
        let totalLength = 0;
        let sliceIndex = 0;

        const closeAndReject = (message: string) => {
          file.closeAsync();
          reject(new Error(message));
        };

        const readNextSlice = () => {
          file.getSliceAsync(sliceIndex, sliceResult => {
            if (sliceResult.status !== Office.AsyncResultStatus.Succeeded) {
              closeAndReject(sliceResult.error.message);
              return;
            }

            const chunk = toByteArray(sliceResult.value.data);
            chunks.push(chunk);
            totalLength += chunk.length;
            sliceIndex += 1;
            setStatus(`Read ${sliceIndex} of ${file.sliceCount} document slices.`, "Reading", "busy");

            if (sliceIndex < file.sliceCount) {
              readNextSlice();
              return;
            }

            file.closeAsync();
            resolve(bytesToBase64(combineChunks(chunks, totalLength)));
          });
        };

        readNextSlice();
      });
  });
}

function toByteArray(data: number[] | Uint8Array): Uint8Array {
  return data instanceof Uint8Array ? data : new Uint8Array(data);
}

function combineChunks(chunks: Uint8Array[], totalLength: number): Uint8Array {
  const bytes = new Uint8Array(totalLength);
  let offset = 0;
  for (const chunk of chunks) {
    bytes.set(chunk, offset);
    offset += chunk.length;
  }

  return bytes;
}

function bytesToBase64(bytes: Uint8Array): string {
  let binary = "";
  const size = 32_768;
  for (let offset = 0; offset < bytes.length; offset += size) {
    const chunk = bytes.subarray(offset, offset + size);
    binary += String.fromCharCode(...chunk);
  }

  return btoa(binary);
}

function base64ToBytes(value: string): Uint8Array {
  const binary = atob(value);
  const bytes = new Uint8Array(binary.length);
  for (let index = 0; index < binary.length; index += 1) {
    bytes[index] = binary.charCodeAt(index);
  }

  return bytes;
}

function currentDocumentFileName(): string {
  if (typeof Office === "undefined") {
    return "protocol.docx";
  }

  const url = Office.context.document.url;
  const name = url.split(/[\\/]/).pop();
  return name && name.toLowerCase().endsWith(".docx") ? name : "protocol.docx";
}

function normalizeServerUrl(value: string): string {
  const trimmed = value.trim();
  if (trimmed.length === 0) {
    throw new Error("BeamKit CI server URL is required.");
  }

  return trimmed.replace(/\/+$/, "");
}

function persistSettings(): void {
  localStorage.setItem(storageKeys.serverUrl, normalizeServerUrl(serverUrlInput.value));
  localStorage.setItem(storageKeys.apiKey, apiKeyInput.value.trim());
  localStorage.setItem(storageKeys.includeSource, String(includeSourceInput.checked));
}

function setBusy(isBusy: boolean): void {
  insertScaffoldButton.disabled = isBusy;
  repairTablesButton.disabled = isBusy;
  refreshLibrariesButton.disabled = isBusy;
  applyTemplateButton.disabled = isBusy;
  addSnippetButton.disabled = isBusy;
  applyMetadataButton.disabled = isBusy;
  addRowButton.disabled = isBusy;
  quickCheckButton.disabled = isBusy;
  extractButton.disabled = isBusy;
  publishDraftButton.disabled = isBusy;
  openDraftButton.disabled = isBusy || !lastDraftReviewUrl;
}

function setStatus(message: string, badge: string, state: "idle" | "busy" | "pass" | "fail"): void {
  statusText.textContent = message;
  statusBadge.textContent = badge;
  statusBadge.className = `badge ${state}`;
}

function element<T extends HTMLElement>(id: string): T {
  const value = document.getElementById(id);
  if (!value) {
    throw new Error(`Missing task pane element '${id}'.`);
  }

  return value as T;
}
