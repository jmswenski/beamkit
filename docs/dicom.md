# DICOM

`BeamKit.Dicom` imports DICOM RT objects into BeamKit's vendor-neutral model using the open-source `fo-dicom` library.

Current support:

- RTSTRUCT structure names.
- RTSTRUCT interpreted structure types.
- RTSTRUCT contour presence.
- RTPLAN prescription dose, fraction count, target reference, beam metadata, treatment machine id, technique id, gantry angle, meterset, control-point meterset weight, and jaw-position import.
- RTDOSE dose-grid spacing metadata.
- RTDOSE uncompressed pixel-grid value extraction with dose-grid scaling.
- RTDOSE DVH sequence import when present.
- DVH-derived dose statistics for maximum dose, mean dose, D95%, and V20 Gy.

Current limitations:

- RTPLAN import is metadata-focused and does not yet model MLC leaf geometry, all treatment technique details, or every machine-specific constraint.
- RTDOSE pixel grids are not yet combined with RTSTRUCT contours for voxel-based DVH calculation.
- Imported DICOM data must be independently validated before any clinical use.
