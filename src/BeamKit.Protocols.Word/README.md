# BeamKit.Protocols.Word

`BeamKit.Protocols.Word` extracts RT-PX protocol intent from structured Microsoft Word `.docx` files.

This package is intentionally separate from `BeamKit.Protocols` so the core RT-PX model stays independent of Office/Open XML dependencies.

## Supported Word Pattern

The v0.1 extractor reads deterministic tables, not free text.

Use headings or first-row table markers named:

- `RT-PX Metadata`
- `RT-PX Structures`
- `RT-PX Prescriptions`
- `RT-PX Dose Constraints`
- `RT-PX Plan Checks`
- `RT-PX Workflow`

Every extracted requirement receives a `source` reference such as `RT-PX Dose Constraints, table 4 row 2`, allowing reviewers to trace generated RT-PX back to the protocol document.

## Example

```csharp
using BeamKit.Protocols.Word;

var report = new RtpxWordProtocolExtractor().Extract("protocol.docx");
if (report.IsValid && report.Package is not null)
{
    // Save with RadiotherapyProtocolPackageStore or compile into rule packs.
}
```

## Template And Package Workflow

```csharp
var template = new RtpxWordTemplateGenerator().Create("protocol-template.docx");

var packaged = new RtpxWordPackageStore().Create(
    "protocol.docx",
    "protocol.rtpx.zip",
    includeSourceDocument: false);

var inspection = new RtpxWordPackageStore().Inspect("protocol.rtpx.zip");
```

Portable packages contain `rtpx.json`, `manifest.json`, and `validation-report.json`. When `includeSourceDocument` is enabled, the package also includes `source/<protocol.docx>` and inspection verifies the bundled source hash.
