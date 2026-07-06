# BeamKit.Calculations

`BeamKit.Calculations` automates common radiation oncology dose calculations that are often done by hand or spreadsheet.

Current support:

- Gy/cGy conversion.
- Dose per fraction and total dose.
- BED using the linear-quadratic model.
- EQD2.
- Equivalent physical dose for a target EQD2 and fraction count.
- Cumulative BED/EQD2 across multiple treatment courses.

This package is deterministic and vendor-neutral. It does not read patient data or call treatment-planning-system SDKs.
