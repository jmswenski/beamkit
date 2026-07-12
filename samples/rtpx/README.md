# RT-PX Samples

RT-PX samples are PHI-free examples of Radiotherapy Protocol Exchange packages.

Available samples:

- [`lung-sbrt-v1`](lung-sbrt-v1/rtpx.json): synthetic low-volume RT-PX package with required structures, one SBRT prescription, dose constraints, explicit physics checks, workflow requirements, approval metadata, and source references.

Validate and compile the sample:

```bash
dotnet run --project src/BeamKit.Cli -- rtpx validate \
  --rtpx samples/rtpx/lung-sbrt-v1

dotnet run --project src/BeamKit.Cli -- rtpx compile \
  --rtpx samples/rtpx/lung-sbrt-v1 \
  --output artifacts/rtpx-rule-packs/lung-sbrt-v1
```
