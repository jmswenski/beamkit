# BeamKit.ChangeDetection

Detects meaningful differences between two vendor-neutral BeamKit plans.

This package is intended for workflow automation such as prescription-change alerts, contour-change review, dose-regeneration checks, treatment-vs-QA plan integrity checks, and approval invalidation.

It compares prescription, structures, dose grid, dose calculation model/version, dose metrics, beams, control points, jaw positions, beam models, and jaw-tracking metadata. It depends only on `BeamKit.Core`.
