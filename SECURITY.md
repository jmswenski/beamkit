# Security Policy

BeamKit is not ready for clinical production deployment.

## Reporting Vulnerabilities

Please report security issues privately by opening a GitHub security advisory when the repository is public. Do not create public issues containing exploit details, credentials, protected health information, or patient identifiers.

## Data Handling

- Do not commit patient data.
- Do not commit DICOM files unless they are synthetic and clearly documented as synthetic.
- Do not commit proprietary SDK DLLs or vendor credentials.
- Treat integration logs as sensitive until proven otherwise.
- The CI server screens uploaded BeamKit plan JSON and ESAPI snapshot JSON for obvious patient identifiers by default, but this is not a replacement for upstream de-identification or institutional PHI controls.
- Request-supplied CI server paths are restricted to configured allowed roots by default. Keep `RestrictServerLocalFilePaths=true` for shared environments and expose only approved import or artifact directories through `AllowedServerLocalFilePathRoots`.

## Supported Versions

BeamKit is pre-1.0. Security support is best-effort until the project publishes its first stable release policy.
