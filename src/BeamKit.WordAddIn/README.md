# BeamKit Word Add-in

`BeamKit.WordAddIn` is an Office.js task-pane scaffold for RT-PX authoring in Microsoft Word.

The add-in helps protocol authors create BeamKit-readable RT-PX tables, use server-backed starter disease-site templates and common requirement snippets, read the active `.docx`, post it to a BeamKit CI server, show Word extraction and RT-PX validation issues, summarize the parsed protocol in plain English, publish a draft into the BeamKit CI review queue, open the published draft in the dashboard, and download a generated `.rtpx.zip` package when the protocol is valid.

## Authoring Tools

The task pane can:

- Insert a complete RT-PX table scaffold at the current Word selection.
- Insert editable starter templates for head and neck, lung SBRT, prostate, breast, brain SRS, and palliative bone protocols.
- Append common high-value snippets such as cord max dose, lung V20, mean heart dose, PTV D95, dose grid, beam model, MU per degree, and QA plan match checks.
- Refresh templates and snippets from BeamKit CI authoring libraries.
- Repair recognized RT-PX tables by restoring canonical headers, normalizing row width, and appending missing tables.
- Apply metadata fields to the `RT-PX Metadata` table.
- Append rows to `RT-PX Structures`.
- Append rows to `RT-PX Prescriptions`, including dose-per-fraction calculation from total dose and fraction count.
- Append rows to `RT-PX Dose Constraints`.
- Append rows to `RT-PX Plan Checks`.
- Navigate from BeamKit validation issues to the referenced Word table row when the issue includes source row information.
- Add a Word comment to the referenced row for issue handoff.
- Offer one-click fixes for common metadata and table/header/row-shape issues.
- Run a quick check that validates the Word protocol and renders a summary without generating a zip package.
- Extract a validated RT-PX package when the author is ready to download and hand off the protocol.
- Publish the active Word protocol as a draft managed rule-pack version for dashboard review.
- Show the server review state returned after draft publish and open the dashboard anchor for that draft.

Rows are appended by matching RT-PX table header rows. If a table is missing, insert the scaffold first.

`Repair Tables` does not split merged cells automatically. Merged cells can move clinical intent between columns, so the add-in highlights/comments the table and leaves that edit to the author.

Starter templates and snippets are authoring aids. They are not clinical policy, and every value must be reviewed against the source protocol and local institution policy before use.

## Authoring Libraries

The task pane loads templates and snippets from BeamKit CI:

```http
GET /api/rtpx/authoring/templates
GET /api/rtpx/authoring/snippets
```

The default server libraries live under `src/BeamKit.CiServer/authoring`. A deployment can point to institution-owned JSON files with:

```bash
export BeamKit__CiServer__RtpxAuthoring__TemplateLibraryPath=/path/to/rtpx-templates.json
export BeamKit__CiServer__RtpxAuthoring__SnippetLibraryPath=/path/to/rtpx-snippets.json
```

If the server cannot be reached, the task pane keeps its built-in starter library so local authoring is not blocked.

## Server Contract

The task pane posts to:

```http
POST /api/rtpx/word/extract
```

Request shape:

```json
{
  "fileName": "protocol.docx",
  "docxBase64": "<base64 .docx>",
  "includeSourceDocument": false,
  "generatePackage": true,
  "clientContext": {
    "caller": "BeamKit.WordAddIn"
  }
}
```

Set `generatePackage` to `false` for quick checks. The server still returns extraction details, validation issues, and extracted `rtpx.json`, but skips the generated `rtpxPackageBase64` payload.

Draft publishing posts to:

```http
POST /api/rtpx/word/publish-draft
```

The draft publish response includes the RT-PX acceptance record, imported managed rule-pack version, safety evidence, protocol diff against the active accepted version, durable review state, and a dashboard anchor for review. The dashboard then owns acknowledgement, approval, request-changes, rejection, and promotion actions.

## Local Development

Start BeamKit CI:

```bash
dotnet dev-certs https --trust
export BeamKit__CiServer__Security__ApiKeys__0__Label=local-admin
export BeamKit__CiServer__Security__ApiKeys__0__Key=dev-secret
dotnet run --project src/BeamKit.CiServer --urls https://localhost:5088
```

Start the add-in dev server:

```bash
cd src/BeamKit.WordAddIn
npm install
npm run dev
```

Sideload `manifest.xml` in Word, then set:

- CI server URL: `https://localhost:5088`
- API key: `dev-secret`

Use [WORD-SMOKE-TEST.md](WORD-SMOKE-TEST.md) for the manual Word host smoke-test checklist.

## Notes

- Office add-ins require HTTPS for the task pane. The Vite dev server uses a local development certificate through `@vitejs/plugin-basic-ssl`.
- Use HTTPS for BeamKit CI when calling it from the Word task pane; browser webviews block HTTPS pages from posting to plain HTTP APIs.
- If port `5088` is already occupied, run BeamKit CI on another HTTPS port such as `5089` and enter that URL in the task pane.
- BeamKit CI allows CORS preflight from `https://localhost:3000` and `https://127.0.0.1:3000` for local Word add-in development.
- The `includeSourceDocument` checkbox embeds the source `.docx` in the generated `.rtpx.zip`; use it only when the source document is appropriate to redistribute.
- This scaffold is not a production Office Store package. Production deployment needs hosted assets, tenant deployment policy, identity, audit retention, and clinical validation.
