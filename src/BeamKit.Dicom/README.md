# BeamKit.Dicom

`BeamKit.Dicom` imports DICOM RT objects into BeamKit's vendor-neutral core model.

Current support:

- RTSTRUCT structure names, interpreted types, and contour presence.
- RTPLAN prescription, beam metadata, treatment machine id, technique id, gantry angles, meterset, control-point weights, and jaw positions.
- RTDOSE grid spacing metadata.
- RTDOSE uncompressed pixel-grid value extraction.
- RTDOSE DVH sequence import when DVH data is present.
- DVH-derived dose statistics for maximum dose, mean dose, D95%, and V20 Gy.

The package uses the open-source `fo-dicom` library and does not depend on proprietary treatment-planning-system SDKs.

This is an initial importer, not a commissioned clinical DICOM validation pipeline.
