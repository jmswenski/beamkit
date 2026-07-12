# BeamKit.Protocols.Acceptance

`BeamKit.Protocols.Acceptance` turns a portable RT-PX package into a local, institution-specific rule pack.

It handles:

- Institution acceptance profiles
- Explicit protocol-to-local structure mappings
- Local approval metadata
- Accepted local `rtpx.json` output
- Acceptance reports and implementation worksheets
- Rule-pack compilation when acceptance has no blocking issues
- Optional ESAPI snapshot checks against mapped structures and prescription intent

The ESAPI support uses BeamKit's JSON snapshot model. It does not require Eclipse, ESAPI DLLs, or proprietary software for tests.

## Example

```csharp
using BeamKit.Protocols.Acceptance;

var profile = RtpxInstitutionProfileStore.FromFile("institutions/my-clinic.json");
var report = new RtpxPackageAcceptanceEngine().Accept(
    new RtpxAcceptanceRequest(
        "protocol.rtpx.zip",
        profile,
        "artifacts/accepted/protocol"));
```

With ESAPI snapshot evidence:

```csharp
using BeamKit.Esapi;

var snapshot = EsapiPlanSnapshotJson.FromFile("esapi-plan-snapshot.json");
var report = new RtpxPackageAcceptanceEngine().Accept(
    new RtpxAcceptanceRequest(
        "protocol.rtpx.zip",
        profile,
        "artifacts/accepted/protocol",
        snapshot,
        "esapi-plan-snapshot.json"));
```

CLI equivalent:

```bash
dotnet run --project src/BeamKit.Cli -- rtpx accept-package \
  --package protocol.rtpx.zip \
  --institution samples/rtpx-acceptance/synthetic-hospital.json \
  --esapi-snapshot samples/rtpx-acceptance/synthetic-esapi-snapshot.json \
  --output artifacts/rtpx-accepted/protocol \
  --format markdown
```
