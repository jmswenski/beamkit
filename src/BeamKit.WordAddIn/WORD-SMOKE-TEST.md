# BeamKit Word Add-in Smoke Test

Use this checklist on a machine with Microsoft Word desktop installed. The repository build can verify the task pane bundle, but Word APIs such as document slicing, table insertion, row selection, highlighting, and comments must be checked in a real Word host.

## Prerequisites

- Microsoft Word desktop with Office add-in sideloading enabled.
- A trusted local HTTPS development certificate.
- BeamKit CI running over HTTPS with an API key.
- The Word add-in Vite dev server running over HTTPS.

## Start Services

From the repository root:

```bash
dotnet dev-certs https --trust
export BeamKit__CiServer__Security__ApiKeys__0__Label=local-admin
export BeamKit__CiServer__Security__ApiKeys__0__Key=dev-secret
dotnet run --project src/BeamKit.CiServer --urls https://localhost:5088
```

From `src/BeamKit.WordAddIn`:

```bash
npm install
npm run dev
```

If `5088` is already occupied, start BeamKit CI on another HTTPS port such as `5089` and enter that URL in the task pane.

## Sideload

1. Open Microsoft Word.
2. Sideload `src/BeamKit.WordAddIn/manifest.xml`.
3. Open the BeamKit RT-PX task pane.
4. Set CI server URL to the HTTPS BeamKit CI URL.
5. Set API key to `dev-secret`.

## Authoring Checks

1. Create a new blank Word document.
2. Click `Insert Scaffold`.
3. Confirm the document receives all six RT-PX sections: metadata, structures, prescriptions, dose constraints, plan checks, and workflow.
4. Change metadata fields in the task pane and click `Apply Metadata`.
5. Confirm the Word metadata table reflects the task-pane values.
6. Choose each row type and click `Add Row`.
7. Confirm rows append to the matching RT-PX table without changing headers.
8. Delete a required table header or remove a column, then click `Repair Tables`.
9. Confirm headers are restored and missing RT-PX tables are appended.
10. Merge cells in one RT-PX table and click `Repair Tables`.
11. Confirm the add-in highlights/comments the non-uniform table instead of silently changing it.

## Template Checks

1. In a blank document, choose each disease-site template.
2. Click `Apply Template`.
3. Confirm a complete editable RT-PX block is inserted at the cursor.
4. Confirm task-pane metadata fields update to the selected template.
5. Click `Refresh Libraries`.
6. Confirm the template and snippet selectors remain populated from BeamKit CI.
7. Run `Quick Check`.
8. Confirm the result panel and protocol summary render without enabling a package download.

## Snippet Checks

1. Insert a scaffold or template.
2. Add each snippet.
3. Confirm constraint snippets append to `RT-PX Dose Constraints`.
4. Confirm plan-check snippets append to `RT-PX Plan Checks`.
5. Run `Quick Check` and confirm no row-shape errors were introduced.

## Extraction Checks

1. Use a valid scaffold or template.
2. Click `Quick Check`.
3. Confirm the task pane shows pass/review status, issue count, and protocol summary.
4. Confirm `Download Package` remains disabled after quick check.
5. Click `Extract RT-PX`.
6. Confirm `Download Package` is enabled when the protocol is valid.
7. Click `Download Package`.
8. Confirm a `.rtpx.zip` file downloads and contains `rtpx.json`, `manifest.json`, and `validation-report.json`.

## Draft Publish Checks

1. Use a valid scaffold or template.
2. Click `Publish Draft`.
3. Confirm the task pane shows the draft review state, acceptance id, rule-pack id, version id, package fingerprint, and protocol-diff count.
4. Click `Open Draft Review`.
5. Confirm the draft appears in `RT-PX Draft Review`.
6. Click `View` and confirm validation, test evidence, safety evidence, and protocol diff are returned.
7. Enter a review note, click `Review`, and confirm the draft state becomes `In review`.
8. Click `Ack Diff`, then `Approve`, and confirm the draft state becomes `Approved` when all gates are satisfied.
9. If the draft is promotable, click `Promote` and confirm the managed rule-pack version becomes active.
10. For a separate draft, click `Changes` or `Reject` and confirm the persisted review state changes.

## Issue Navigation Checks

1. Create a known authoring issue, such as an invalid dose constraint comparison or a missing required metadata field.
2. Run `Quick Check`.
3. Click `Go` on an issue that includes table row context.
4. Confirm Word selects/highlights the referenced row.
5. Click `Comment`.
6. Confirm Word adds a comment to the referenced row.
7. For metadata or table-shape issues, click the suggested fix button.
8. Confirm the appropriate metadata or table repair action runs.

## Expected Result

The add-in should let a protocol author start from an editable scaffold or server-backed starter template, add common requirements, validate the active Word document through BeamKit CI, understand the extracted protocol from a concise summary, publish a reviewable draft, and package a valid RT-PX artifact without leaving Word.
