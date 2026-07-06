# Deliverability Checks

`BeamKit.Deliverability` evaluates beam-level machine constraints from vendor-neutral `BeamKit.Core` beam metadata.

The first implementation focuses on checks that are commonly reviewed manually or through treatment-planning-system scripts:

- Minimum monitor units per treatment beam.
- Minimum monitor units per control-point interval.
- Minimum monitor units per gantry degree for arcs.
- Maximum jaw-defined field size, including FFF-specific limits.
- Minimum jaw opening.
- Maximum DCA control-point step size.
- Allowed machine, energy, technique, beam model, calculation model, and calculation version.
- Required jaw-tracking state when source data provides it.

## Machine Profiles

Machine limits are stored in JSON:

```json
{
  "name": "Synthetic linear accelerator constraints",
  "version": "2026.1",
  "machineId": "SYN-LINAC",
  "beamModelId": "SYN-AAA-6X",
  "calculationModel": "SyntheticAAA",
  "calculationModelVersion": "16.1",
  "allowedEnergies": [ "6X" ],
  "allowedTechniques": [ "VMAT" ],
  "allowedBeamModelIds": [ "SYN-AAA-6X" ],
  "minMonitorUnitsPerDegree": 0.1,
  "monitorUnitsPerDegreeConstraints": [
    {
      "machineId": "SYN-LINAC",
      "energy": "6X",
      "techniqueId": "VMAT",
      "diseaseSite": "Head and Neck",
      "minMonitorUnitsPerDegree": 0.1
    }
  ],
  "minMonitorUnitsPerSegment": 0.1,
  "minMonitorUnitsPerBeam": 40,
  "minJawOpeningCm": 0.5,
  "maxOpenFieldSizeCm": 40,
  "maxMlcFieldSizeCm": 22,
  "maxFffFieldSizeCm": 15,
  "maxDcaStepSizeDegrees": 5,
  "requireJawTracking": true
}
```

The schema is available at [schemas/machine-profile.schema.json](../schemas/machine-profile.schema.json).

## CLI

```bash
dotnet run --project src/BeamKit.Cli -- deliverability \
  --plan samples/synthetic-plan.json \
  --machine-profile samples/machine-profile-synthetic.json \
  --format markdown
```

The command exits with code `2` when a check fails or cannot be evaluated.

## Adapter Expectations

Adapters should populate:

- Beam MU.
- Treatment unit and technique identifiers when available.
- Beam model identifiers when available.
- Dose calculation model and version when available.
- Jaw-tracking state when available.
- Control-point indices.
- Cumulative meterset weights.
- Gantry angles.
- Jaw positions in centimeters.

`BeamKit.Deliverability` does not call DICOM, ESAPI, or any proprietary API directly.
