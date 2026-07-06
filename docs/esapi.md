# ESAPI Adapter

`BeamKit.Esapi` provides a read-only adapter scaffold for Eclipse ESAPI workflows.

The package does not reference Varian ESAPI DLLs. Instead, caller-owned ESAPI code extracts data into snapshot records:

- `EsapiPlanSnapshot`
- `EsapiPrescriptionSnapshot`
- `EsapiStructureSnapshot`
- `EsapiDoseGridSnapshot`
- `EsapiDoseStatisticsSnapshot`
- `EsapiBeamSnapshot`

`EsapiPlanConverter` converts those snapshots into `BeamKit.Core.Plan`.

This keeps BeamKit's default build and tests runnable on Linux without proprietary software.
