# RT-PX Word Samples

This folder contains pasteable table examples for building a Word `.docx` protocol that BeamKit can convert to RT-PX.

BeamKit does not currently check binary `.docx` files into the repository. The tests generate Word documents at runtime so the sample suite remains text-reviewable and PHI-free.

## Usage

1. Open Microsoft Word.
2. Create headings named `RT-PX Metadata`, `RT-PX Structures`, `RT-PX Prescriptions`, `RT-PX Dose Constraints`, `RT-PX Plan Checks`, and `RT-PX Workflow`.
3. Paste the tables from [`lung-sbrt-word-tables.md`](lung-sbrt-word-tables.md) under the matching headings.
4. Save the document as `.docx`.
5. Run:

```bash
dotnet run --project src/BeamKit.Cli -- rtpx lint-word --docx protocol.docx
dotnet run --project src/BeamKit.Cli -- rtpx extract-word --docx protocol.docx --output artifacts/rtpx/lung-sbrt/rtpx.json
```
