# DICOM

`BeamKit.Dicom` imports DICOM RT objects into BeamKit's vendor-neutral model using the open-source `fo-dicom` library.

Current support:

- RTSTRUCT structure names.
- RTSTRUCT interpreted structure types.
- RTSTRUCT contour presence.
- RTDOSE dose-grid spacing metadata.
- RTDOSE DVH sequence import when present.
- DVH-derived dose statistics for maximum dose, mean dose, D95%, and V20 Gy.

Current limitations:

- RTPLAN import is not implemented yet.
- RTDOSE pixel grid decoding is not yet used for voxel-based DVH calculation.
- Imported DICOM data must be independently validated before any clinical use.
