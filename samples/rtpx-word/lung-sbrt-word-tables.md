# Lung SBRT RT-PX Word Tables

Paste each table into a Word document under a matching heading.

## RT-PX Metadata

| Field | Value |
| --- | --- |
| Id | rtpx.synthetic.lung-sbrt |
| Name | Synthetic Lung SBRT |
| Version | 1.0.0 |
| Disease Site | Lung |
| Intent | Definitive |
| Status | Approved |
| Reviewed By | Physics Reviewer |
| Approved By | Protocol Chair |
| Effective Date | 2026-07-12 |
| Owner | BeamKit sample |
| Tags | sbrt; synthetic; word-source |

## RT-PX Structures

| Id | Name | Role | Level | Aliases | Must Have Contours | Description |
| --- | --- | --- | --- | --- | --- | --- |
| ptv | PTV_5000 | Target | Required | PTV; Planning Target Volume | yes | Primary planning target |
| cord | Cord | OAR | Required | SpinalCord | yes | Cord organ at risk |

## RT-PX Prescriptions

| Id | Target | Total Dose Gy | Fractions | Dose Per Fraction Gy | Technique | Energy | Level | Description |
| --- | --- | ---: | ---: | ---: | --- | --- | --- | --- |
| rx.primary | PTV_5000 | 54 | 5 | 10.8 | VMAT | 6X | Required | Primary prescription |

## RT-PX Dose Constraints

| Id | Structure | Metric | Comparison | Value | Unit | Level | Description | Active |
| --- | --- | --- | --- | ---: | --- | --- | --- | --- |
| cord.max | Cord | Max | <= | 30 | Gy | Required | Cord max dose | yes |

## RT-PX Plan Checks

| Id | Title | Type | Level | Parameters | Description | Active |
| --- | --- | --- | --- | --- | --- | --- |
| dose-grid | Dose grid <= 2.5 mm | DoseGridResolution | Required | maxMm=2.5 | Protocol grid check | yes |

## RT-PX Workflow

| Id | Title | Type | Level | Description | Active |
| --- | --- | --- | --- | --- | --- |
| physics.review | Physics review before treatment | Approval | Required | Protocol cases need physics review | yes |
