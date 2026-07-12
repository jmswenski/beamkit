# RT-PX Acceptance Samples

This directory contains PHI-free fixtures for accepting a portable RT-PX package at a treating institution.

- `synthetic-hospital.json` is a local institution profile with structure mappings and approval metadata.
- `synthetic-esapi-snapshot.json` is a vendor-neutral BeamKit ESAPI snapshot that matches the default RT-PX Word template.

Example:

```bash
dotnet run --project src/BeamKit.Cli -- rtpx template-word \
  --output artifacts/rtpx-acceptance/protocol.docx \
  --overwrite

dotnet run --project src/BeamKit.Cli -- rtpx package-word \
  --docx artifacts/rtpx-acceptance/protocol.docx \
  --output artifacts/rtpx-acceptance/protocol.rtpx.zip \
  --overwrite

dotnet run --project src/BeamKit.Cli -- rtpx accept-package \
  --package artifacts/rtpx-acceptance/protocol.rtpx.zip \
  --institution samples/rtpx-acceptance/synthetic-hospital.json \
  --esapi-snapshot samples/rtpx-acceptance/synthetic-esapi-snapshot.json \
  --output artifacts/rtpx-acceptance/accepted \
  --overwrite \
  --format markdown
```
