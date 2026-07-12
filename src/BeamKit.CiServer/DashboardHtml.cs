namespace BeamKit.CiServer;

internal static class DashboardHtml
{
    public const string Content = """
        <!doctype html>
        <html lang="en">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1">
          <title>BeamKit CI Server</title>
          <style>
            :root {
              color-scheme: light;
              --bg: #f7f8fa;
              --surface: #ffffff;
              --text: #17202a;
              --muted: #5d6d7e;
              --line: #d9dee7;
              --accent: #0b6bcb;
              --good: #177245;
              --bad: #b42318;
              --warn: #8a5a00;
            }

            * { box-sizing: border-box; }
            body {
              margin: 0;
              font-family: ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
              background: var(--bg);
              color: var(--text);
            }

            header {
              background: var(--surface);
              border-bottom: 1px solid var(--line);
              padding: 20px 28px;
            }

            h1, h2 { margin: 0; letter-spacing: 0; }
            h1 { font-size: 24px; line-height: 1.25; }
            h2 { font-size: 16px; line-height: 1.35; }
            p { color: var(--muted); line-height: 1.5; margin: 8px 0 0; }

            main {
              display: grid;
              grid-template-columns: minmax(280px, 380px) minmax(0, 1fr);
              gap: 20px;
              padding: 20px 28px 32px;
            }

            section, aside {
              background: var(--surface);
              border: 1px solid var(--line);
              border-radius: 8px;
              padding: 16px;
            }

            .stack { display: grid; gap: 12px; }
            .actions { display: flex; flex-wrap: wrap; gap: 8px; margin-top: 14px; }

            button {
              min-height: 36px;
              border: 1px solid #0b5cad;
              background: var(--accent);
              color: white;
              border-radius: 6px;
              padding: 0 12px;
              font-weight: 650;
              cursor: pointer;
            }

            button.secondary {
              background: white;
              color: var(--accent);
            }

            label {
              display: grid;
              gap: 5px;
              color: var(--muted);
              font-size: 13px;
              font-weight: 650;
            }

            input, select, textarea {
              width: 100%;
              min-height: 36px;
              border: 1px solid var(--line);
              border-radius: 6px;
              padding: 0 10px;
              color: var(--text);
              background: white;
            }

            textarea {
              min-height: 72px;
              padding: 8px 10px;
              resize: vertical;
              font: inherit;
            }

            table {
              width: 100%;
              border-collapse: collapse;
              margin-top: 12px;
              font-size: 13px;
            }

            th, td {
              border-bottom: 1px solid var(--line);
              padding: 9px 8px;
              text-align: left;
              vertical-align: top;
            }

            th { color: var(--muted); font-size: 12px; text-transform: uppercase; }
            code { font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace; }
            pre {
              overflow: auto;
              max-height: 420px;
              background: #101828;
              color: #eef2f8;
              border-radius: 8px;
              padding: 12px;
              font-size: 12px;
              line-height: 1.45;
            }

            .status-pass { color: var(--good); font-weight: 700; }
            .status-warning { color: var(--warn); font-weight: 700; }
            .status-fail { color: var(--bad); font-weight: 700; }

            @media (max-width: 900px) {
              main { grid-template-columns: 1fr; padding: 16px; }
              header { padding: 16px; }
            }
          </style>
        </head>
        <body>
          <header>
            <h1>BeamKit CI Server</h1>
            <p>Self-hosted plan gates, rule-pack validation, regression tests, provenance artifacts, and assignment recommendations.</p>
          </header>
          <main>
            <aside class="stack">
              <section>
                <h2>Access</h2>
                <div class="stack" style="margin-top:12px">
                  <label>API key
                    <input id="apiKey" type="password" autocomplete="off" oninput="saveApiKey()">
                  </label>
                </div>
              </section>
              <section>
                <h2>Run Gate</h2>
                <div class="stack" style="margin-top:12px">
                  <label>Synthetic case
                    <input id="caseId" value="head-neck-pass">
                  </label>
                  <label>Branch
                    <input id="branch" value="main">
                  </label>
                  <label>Commit
                    <input id="commit" value="local-demo">
                  </label>
                  <label>Build ID
                    <input id="buildId" value="beamkit-local">
                  </label>
                  <label>Rule pack ID
                    <input id="rulePackId" placeholder="synthetic-head-neck">
                  </label>
                </div>
                <div class="actions">
                  <button onclick="createRun()">Run</button>
                  <button class="secondary" onclick="runFailingCase()">Run failing case</button>
                </div>
              </section>
              <section>
                <h2>Policy</h2>
                <div class="actions">
                  <button onclick="validatePolicy()">Validate</button>
                  <button class="secondary" onclick="testPolicy()">Test rule pack</button>
                </div>
              </section>
              <section>
                <h2>RT-PX Acceptance</h2>
                <div class="stack" style="margin-top:12px">
                  <label>Package path
                    <input id="rtpxPackagePath" placeholder="protocol.rtpx.zip">
                  </label>
                  <label>Institution profile
                    <input id="rtpxInstitutionPath" placeholder="institution.json">
                  </label>
                  <label>ESAPI snapshot
                    <input id="rtpxEsapiPath" placeholder="snapshot.json">
                  </label>
                  <label>Synthetic case
                    <input id="rtpxSyntheticCaseId" value="head-neck-pass">
                  </label>
                  <label>
                    <span><input id="rtpxPromote" type="checkbox" style="width:auto; min-height:auto; margin-right:6px">Promote active</span>
                  </label>
                </div>
                <div class="actions">
                  <button onclick="acceptRtpxPackage()">Accept package</button>
                  <button class="secondary" onclick="loadRtpxAcceptances()">Refresh</button>
                </div>
              </section>
              <section>
                <h2>RT-PX Draft Review</h2>
                <div class="stack" style="margin-top:12px">
                  <label>Status filter
                    <select id="rtpxDraftStatusFilter" onchange="loadRtpxDrafts()">
                      <option value="">Any</option>
                      <option value="Draft">Draft</option>
                      <option value="InReview">In review</option>
                      <option value="ChangesRequested">Changes requested</option>
                      <option value="Approved">Approved</option>
                      <option value="Rejected">Rejected</option>
                      <option value="Promoted">Promoted</option>
                    </select>
                  </label>
                  <label>Review note
                    <textarea id="rtpxDraftReviewNote" placeholder="Reviewer, rationale, requested changes, or promotion note"></textarea>
                  </label>
                </div>
                <div class="actions">
                  <button class="secondary" onclick="loadRtpxDrafts()">Refresh drafts</button>
                </div>
              </section>
              <section>
                <h2>Protocol Compliance</h2>
                <div class="stack" style="margin-top:12px">
                  <label>Synthetic case
                    <input id="protocolComplianceCaseId" value="head-neck-pass">
                  </label>
                  <label>Rule pack ID
                    <input id="protocolComplianceRulePackId" placeholder="active RT-PX rule pack">
                  </label>
                  <label>RT-PX acceptance ID
                    <input id="protocolComplianceAcceptanceId" placeholder="optional rtpx-...">
                  </label>
                  <label>Variance finding ID
                    <input id="protocolComplianceFindingId" placeholder="plancheck:cord-max">
                  </label>
                  <label>Variance rationale
                    <textarea id="protocolComplianceVarianceRationale" placeholder="Clinical or physics rationale"></textarea>
                  </label>
                </div>
                <div class="actions">
                  <button onclick="runProtocolCompliance()">Run compliance</button>
                  <button class="secondary" onclick="acceptProtocolComplianceVariance()">Accept variance</button>
                  <button class="secondary" onclick="loadProtocolComplianceRuns()">Refresh</button>
                </div>
              </section>
              <section>
                <h2>Assignment</h2>
                <div class="stack" style="margin-top:12px">
                  <label>Disease site
                    <input id="diseaseSite" value="Head and Neck">
                  </label>
                  <label>Synthetic case
                    <input id="assignmentCaseId" placeholder="lung-sbrt-pass">
                  </label>
                  <label>Required skill
                    <input id="requiredSkill" placeholder="VMAT">
                  </label>
                  <label>Physician
                    <input id="assignmentPhysician" placeholder="Dr Smith">
                  </label>
                  <label>Roster path
                    <input id="assignmentRosterPath" value="samples/staff-roster-synthetic.json">
                  </label>
                </div>
                <div class="actions">
                  <button onclick="recommendAssignment()">Recommend</button>
                  <button class="secondary" onclick="recommendTeam()">Recommend team</button>
                </div>
              </section>
              <section>
                <h2>Work Queue</h2>
                <div class="stack" style="margin-top:12px">
                  <label>Work item ID
                    <input id="workItemId" placeholder="work-...">
                  </label>
                  <label>Queue synthetic case
                    <input id="workSyntheticCaseId" value="lung-sbrt-pass">
                  </label>
                  <label>Queue disease site
                    <input id="workDiseaseSite" value="Lung">
                  </label>
                  <label>Due date
                    <input id="workDueDate" value="2026-07-12">
                  </label>
                  <label>Priority
                    <input id="workPriority" value="4" inputmode="numeric">
                  </label>
                  <label>Dosimetrist ID
                    <input id="workDosimetristId" placeholder="planner-jane">
                  </label>
                  <label>Physicist ID
                    <input id="workPhysicistId" placeholder="physicist-morgan">
                  </label>
                </div>
                <div class="actions">
                  <button onclick="createWorkItem()">Create</button>
                  <button class="secondary" onclick="recommendWorkItem()">Recommend</button>
                  <button class="secondary" onclick="assignWorkItem()">Assign</button>
                </div>
              </section>
            </aside>
            <section>
              <h2>Recent Runs</h2>
              <div class="stack" style="margin-top:12px; grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));">
                <label>Status
                  <select id="filterStatus" onchange="loadRuns()">
                    <option value="">Any</option>
                    <option value="Pass">Pass</option>
                    <option value="Warning">Warning</option>
                    <option value="Fail">Fail</option>
                  </select>
                </label>
                <label>Case
                  <input id="filterCase" placeholder="head-neck-pass" oninput="loadRuns()">
                </label>
                <label>Branch
                  <input id="filterBranch" placeholder="main" oninput="loadRuns()">
                </label>
                <label>Limit
                  <input id="filterLimit" value="50" inputmode="numeric" oninput="loadRuns()">
                </label>
              </div>
              <table>
                <thead>
                  <tr><th>Run</th><th>Source</th><th>Case</th><th>Status</th><th>Snapshot</th><th>Exit</th><th>Created</th><th>Actions</th></tr>
                </thead>
                <tbody id="runs"></tbody>
              </table>
              <h2 style="margin-top:22px">RT-PX Acceptance</h2>
              <table>
                <thead>
                  <tr><th>Acceptance</th><th>Protocol</th><th>Institution</th><th>Status</th><th>Rule Pack</th><th>Evidence</th><th>Created</th><th>Actions</th></tr>
                </thead>
                <tbody id="rtpxAcceptances"></tbody>
              </table>
              <h2 style="margin-top:22px">RT-PX Draft Review</h2>
              <table>
                <thead>
                  <tr><th>Draft</th><th>Protocol</th><th>Status</th><th>Rule Pack</th><th>Validation</th><th>Diff</th><th>Evidence</th><th>Actions</th></tr>
                </thead>
                <tbody id="rtpxDrafts"></tbody>
              </table>
              <h2 id="rtpxDraftDiffTitle" style="margin-top:22px">Selected Draft Diff</h2>
              <table>
                <thead>
                  <tr><th>Change</th><th>Severity</th><th>Status</th><th>Before</th><th>After</th></tr>
                </thead>
                <tbody id="rtpxDraftDiffs">
                  <tr><td colspan="5">Select a draft to view protocol differences.</td></tr>
                </tbody>
              </table>
              <h2 style="margin-top:22px">Protocol Compliance</h2>
              <table>
                <thead>
                  <tr><th>Run</th><th>Plan</th><th>Status</th><th>Protocol</th><th>Rule Pack</th><th>Findings</th><th>Created</th><th>Actions</th></tr>
                </thead>
                <tbody id="protocolComplianceRuns"></tbody>
              </table>
              <h2 style="margin-top:22px">Work Queue</h2>
              <table>
                <thead>
                  <tr><th>Item</th><th>Case</th><th>Status</th><th>Disease</th><th>Due</th><th>Dosimetrist</th><th>Physicist</th><th>Updated</th><th>Actions</th></tr>
                </thead>
                <tbody id="workItems"></tbody>
              </table>
              <h2 style="margin-top:22px">Response</h2>
              <pre id="output">{}</pre>
            </section>
          </main>
          <script>
            const apiKeyInput = document.getElementById("apiKey");
            apiKeyInput.value = localStorage.getItem("beamkitApiKey") || "";

            function saveApiKey() {
              localStorage.setItem("beamkitApiKey", apiKeyInput.value);
            }

            function requestHeaders() {
              const headers = { "content-type": "application/json" };
              const apiKey = apiKeyInput.value.trim();
              if (apiKey) headers["X-BeamKit-Api-Key"] = apiKey;
              return headers;
            }

            async function api(path, options) {
              const response = await fetch(path, {
                headers: requestHeaders(),
                ...options
              });
              const text = await response.text();
              const data = text ? JSON.parse(text) : null;
              document.getElementById("output").textContent = JSON.stringify(data, null, 2);
              await loadRuns();
              await loadRtpxAcceptances();
              await loadRtpxDrafts();
              await loadProtocolComplianceRuns();
              await loadWorkItems();
              return data;
            }

            function escapeHtml(value) {
              return String(value ?? "").replace(/[&<>"']/g, ch => {
                switch (ch) {
                  case "&": return "&amp;";
                  case "<": return "&lt;";
                  case ">": return "&gt;";
                  case '"': return "&quot;";
                  case "'": return "&#39;";
                  default: return ch;
                }
              });
            }

            function escapeOnclickString(value) {
              return escapeHtml(String(value ?? "")
                .replace(/\\/g, "\\\\")
                .replace(/'/g, "\\'")
                .replace(/\r/g, "\\r")
                .replace(/\n/g, "\\n"));
            }

            async function createRun() {
              await api("/api/runs", {
                method: "POST",
                body: JSON.stringify({
                  syntheticCaseId: document.getElementById("caseId").value,
                  branch: document.getElementById("branch").value,
                  commit: document.getElementById("commit").value,
                  buildId: document.getElementById("buildId").value,
                  rulePackId: document.getElementById("rulePackId").value || null
                })
              });
            }

            async function runFailingCase() {
              document.getElementById("caseId").value = "head-neck-cord-fail";
              await createRun();
            }

            async function validatePolicy() {
              await api("/api/rule-packs/validate", { method: "POST", body: "{}" });
            }

            async function testPolicy() {
              await api("/api/rule-packs/test", { method: "POST", body: "{}" });
            }

            async function acceptRtpxPackage() {
              const packagePath = document.getElementById("rtpxPackagePath").value.trim();
              const institutionProfilePath = document.getElementById("rtpxInstitutionPath").value.trim();
              if (!packagePath || !institutionProfilePath) return;
              await api("/api/rtpx/acceptance", {
                method: "POST",
                body: JSON.stringify({
                  packagePath,
                  institutionProfilePath,
                  esapiSnapshotPath: document.getElementById("rtpxEsapiPath").value.trim() || null,
                  rulePackId: document.getElementById("rulePackId").value || null,
                  syntheticCaseId: document.getElementById("rtpxSyntheticCaseId").value || null,
                  promote: document.getElementById("rtpxPromote").checked,
                  importedBy: "dashboard",
                  note: "Accepted from local BeamKit CI dashboard."
                })
              });
            }

            async function runProtocolCompliance() {
              const payload = {
                syntheticCaseId: document.getElementById("protocolComplianceCaseId").value || null,
                rulePackId: document.getElementById("protocolComplianceRulePackId").value || null,
                rtpxAcceptanceId: document.getElementById("protocolComplianceAcceptanceId").value || null,
                inputSource: "dashboard"
              };
              await api("/api/protocol-compliance/runs", {
                method: "POST",
                body: JSON.stringify(payload)
              });
            }

            async function acceptProtocolComplianceVariance() {
              const runId = document.getElementById("protocolComplianceRunId")?.value || "";
              const selectedRunId = runId || document.getElementById("protocolComplianceFindingId").dataset.runId || "";
              const findingId = document.getElementById("protocolComplianceFindingId").value.trim();
              if (!selectedRunId || !findingId) return;
              await api(`/api/protocol-compliance/runs/${selectedRunId}/variances`, {
                method: "POST",
                body: JSON.stringify({
                  findingId,
                  acceptedBy: "dashboard",
                  rationale: document.getElementById("protocolComplianceVarianceRationale").value || "Accepted from local BeamKit CI dashboard."
                })
              });
            }

            async function recommendAssignment() {
              const requiredSkill = document.getElementById("requiredSkill").value;
              await api("/api/assignments/recommend", {
                method: "POST",
                body: JSON.stringify({
                  diseaseSite: document.getElementById("diseaseSite").value || null,
                  syntheticCaseId: document.getElementById("assignmentCaseId").value || null,
                  requiredSkills: requiredSkill ? [requiredSkill] : null,
                  physician: document.getElementById("assignmentPhysician").value || null,
                  rosterPath: document.getElementById("assignmentRosterPath").value || null
                })
              });
            }

            async function recommendTeam() {
              const requiredSkill = document.getElementById("requiredSkill").value;
              await api("/api/assignments/recommend-team", {
                method: "POST",
                body: JSON.stringify({
                  diseaseSite: document.getElementById("diseaseSite").value || null,
                  syntheticCaseId: document.getElementById("assignmentCaseId").value || null,
                  requiredSkills: requiredSkill ? [requiredSkill] : null,
                  physician: document.getElementById("assignmentPhysician").value || null,
                  rosterPath: document.getElementById("assignmentRosterPath").value || null
                })
              });
            }

            async function createWorkItem() {
              const data = await api("/api/work-items", {
                method: "POST",
                body: JSON.stringify({
                  syntheticCaseId: document.getElementById("workSyntheticCaseId").value || null,
                  diseaseSite: document.getElementById("workDiseaseSite").value || null,
                  dueDate: document.getElementById("workDueDate").value || null,
                  priority: Number(document.getElementById("workPriority").value || "3"),
                  physician: document.getElementById("assignmentPhysician").value || null,
                  rulePackId: document.getElementById("rulePackId").value || null
                })
              });
              if (data && data.id) document.getElementById("workItemId").value = data.id;
            }

            async function recommendWorkItem() {
              const workItemId = document.getElementById("workItemId").value.trim();
              if (!workItemId) return;
              await api(`/api/work-items/${workItemId}/recommend-assignment`, {
                method: "POST",
                body: JSON.stringify({
                  rosterPath: document.getElementById("assignmentRosterPath").value || null,
                  physician: document.getElementById("assignmentPhysician").value || null
                })
              });
            }

            async function assignWorkItem() {
              const workItemId = document.getElementById("workItemId").value.trim();
              if (!workItemId) return;
              await api(`/api/work-items/${workItemId}/assign`, {
                method: "POST",
                body: JSON.stringify({
                  dosimetristId: document.getElementById("workDosimetristId").value || null,
                  physicistId: document.getElementById("workPhysicistId").value || null,
                  note: "Assigned from local BeamKit CI dashboard."
                })
              });
            }

            async function promoteBaseline(runId) {
              await api(`/api/runs/${runId}/baseline`, {
                method: "POST",
                body: JSON.stringify({
                  promotedBy: "dashboard",
                  note: "Promoted from local BeamKit CI dashboard."
                })
              });
            }

            async function compareBaseline(runId) {
              await api(`/api/runs/${runId}/baseline-comparison`);
            }

            async function downloadArtifact(runId) {
              const response = await fetch(`/api/runs/${runId}/artifact/download`, { headers: requestHeaders() });
              if (!response.ok) {
                const text = await response.text();
                document.getElementById("output").textContent = text;
                return;
              }

              const blob = await response.blob();
              const url = URL.createObjectURL(blob);
              const link = document.createElement("a");
              link.href = url;
              link.download = `${runId}.beamkit-ci-artifact.json`;
              link.click();
              URL.revokeObjectURL(url);
            }

            async function loadRuns() {
              const params = new URLSearchParams();
              const status = document.getElementById("filterStatus").value;
              const caseId = document.getElementById("filterCase").value;
              const branch = document.getElementById("filterBranch").value;
              const limit = document.getElementById("filterLimit").value;
              if (status) params.set("status", status);
              if (caseId) params.set("caseId", caseId);
              if (branch) params.set("branch", branch);
              if (limit) params.set("limit", limit);
              const response = await fetch(`/api/runs?${params}`, { headers: requestHeaders() });
              if (!response.ok) {
                const text = await response.text();
                document.getElementById("output").textContent = text;
                document.getElementById("runs").innerHTML = "";
                return;
              }

              const runs = await response.json();
              document.getElementById("runs").innerHTML = runs.map(run => {
                const runId = escapeOnclickString(run.id);
                const status = String(run.status || "").toLowerCase();
                return `<tr>
                  <td><code>${escapeHtml(run.id)}</code></td>
                  <td>${escapeHtml(formatInputKind(run.inputKind))}</td>
                  <td>${escapeHtml(run.caseId || run.syntheticCaseId || "")}</td>
                  <td class="status-${escapeHtml(status)}">${escapeHtml(run.status)}</td>
                  <td>${run.hasPlanSnapshot ? "Plan" : "Metadata"}</td>
                  <td>${escapeHtml(run.exitCode)}</td>
                  <td>${escapeHtml(new Date(run.createdAtUtc).toLocaleString())}</td>
                  <td>
                    <button class="secondary" style="min-height:28px; padding:0 8px" onclick="downloadArtifact('${runId}')">JSON</button>
                    <button class="secondary" style="min-height:28px; padding:0 8px; margin-left:6px" onclick="promoteBaseline('${runId}')">Baseline</button>
                    <button class="secondary" style="min-height:28px; padding:0 8px; margin-left:6px" onclick="compareBaseline('${runId}')">Compare</button>
                  </td>
                </tr>`;
              }).join("");
            }

            async function loadWorkItems() {
              const response = await fetch("/api/work-items?limit=50", { headers: requestHeaders() });
              if (!response.ok) {
                document.getElementById("workItems").innerHTML = "";
                return;
              }

              const items = await response.json();
              document.getElementById("workItems").innerHTML = items.map(item => {
                const workItemId = escapeOnclickString(item.id);
                return `<tr>
                  <td><code>${escapeHtml(item.id)}</code></td>
                  <td>${escapeHtml(item.caseId)}</td>
                  <td>${escapeHtml(item.status)}</td>
                  <td>${escapeHtml(item.diseaseSite || "")}</td>
                  <td>${escapeHtml(item.dueDate || "")}</td>
                  <td>${escapeHtml(item.assignedDosimetristName || item.assignedDosimetristId || "")}</td>
                  <td>${escapeHtml(item.assignedPhysicistName || item.assignedPhysicistId || "")}</td>
                  <td>${escapeHtml(new Date(item.updatedAtUtc).toLocaleString())}</td>
                  <td><button class="secondary" style="min-height:28px; padding:0 8px" onclick="selectWorkItem('${workItemId}')">Use</button></td>
                </tr>`;
              }).join("");
            }

            async function loadRtpxAcceptances() {
              const response = await fetch("/api/rtpx/acceptance?limit=25", { headers: requestHeaders() });
              if (!response.ok) {
                document.getElementById("rtpxAcceptances").innerHTML = "";
                return;
              }

              const records = await response.json();
              document.getElementById("rtpxAcceptances").innerHTML = records.map(record => {
                const recordId = escapeOnclickString(record.id);
                const status = record.accepted ? (record.promoted ? "Active" : "Accepted") : "Rejected";
                const statusClass = record.accepted ? "status-pass" : "status-fail";
                const rulePack = record.rulePackId
                  ? `<code>${escapeHtml(record.rulePackId)}</code><br><code>${escapeHtml(record.versionId || "")}</code>`
                  : "";
                return `<tr>
                  <td><code>${escapeHtml(record.id)}</code></td>
                  <td>${escapeHtml(record.sourceProtocolName)}<br><code>${escapeHtml(record.sourceProtocolVersion)}</code></td>
                  <td>${escapeHtml(record.institution)}</td>
                  <td class="${statusClass}">${status}</td>
                  <td>${rulePack}</td>
                  <td>${record.hasEsapiEvidence ? "ESAPI" : "Package"}<br>${escapeHtml(record.errorCount)} error / ${escapeHtml(record.warningCount)} warn</td>
                  <td>${escapeHtml(new Date(record.createdAtUtc).toLocaleString())}</td>
                  <td><button class="secondary" style="min-height:28px; padding:0 8px" onclick="viewRtpxAcceptance('${recordId}')">View</button></td>
                </tr>`;
              }).join("");
            }

            async function loadRtpxDrafts() {
              const response = await fetch("/api/rtpx/drafts?limit=100", { headers: requestHeaders() });
              if (!response.ok) {
                document.getElementById("rtpxDrafts").innerHTML = "";
                return;
              }

              const statusFilter = document.getElementById("rtpxDraftStatusFilter").value;
              const drafts = (await response.json()).filter(draft => {
                const status = draft.acceptance?.reviewStatus || "";
                return !statusFilter || status === statusFilter;
              });
              document.getElementById("rtpxDrafts").innerHTML = drafts.map(draft => {
                const acceptance = draft.acceptance || {};
                const version = draft.version || {};
                const diff = draft.protocolDiff || {};
                const draftId = escapeOnclickString(acceptance.id);
                const validationClass = version.isValid ? "status-pass" : "status-fail";
                const testText = version.testPassed === true ? "tests pass" : version.testPassed === false ? "tests fail" : "tests not run";
                const reviewStatus = acceptance.reviewStatus || "Draft";
                const required = draft.acknowledgementRequiredChanges?.length || 0;
                const pending = draft.pendingAcknowledgementChanges?.length || 0;
                const diffText = diff.isInitial
                  ? "Initial package"
                  : `${escapeHtml(diff.changeCount || 0)} change(s)`;
                const ackText = required === 0 ? "no acknowledgement needed" : `${required - pending}/${required} acknowledged`;
                return `<tr id="rtpx-draft-${escapeHtml(acceptance.id)}">
                  <td><code>${escapeHtml(acceptance.id)}</code><br>${escapeHtml(new Date(acceptance.createdAtUtc).toLocaleString())}</td>
                  <td>${escapeHtml(acceptance.sourceProtocolName)}<br><code>${escapeHtml(acceptance.sourceProtocolVersion)}</code></td>
                  <td>${escapeHtml(formatDraftStatus(reviewStatus))}<br>${escapeHtml(acceptance.reviewedBy || "")}</td>
                  <td><code>${escapeHtml(acceptance.rulePackId || "")}</code><br><code>${escapeHtml(acceptance.versionId || "")}</code></td>
                  <td class="${validationClass}">${version.isValid ? "valid" : "invalid"}<br>${escapeHtml(testText)}</td>
                  <td>${diffText}<br>${escapeHtml(ackText)}</td>
                  <td>${draft.safetyEvidence ? "safety evidence" : "missing"}<br>${draft.isPromotable ? "promotable" : draft.isApprovable ? "approvable" : "needs review"}</td>
                  <td>
                    <button class="secondary" style="min-height:28px; padding:0 8px" onclick="viewRtpxDraft('${draftId}')">View</button>
                    <button class="secondary" style="min-height:28px; padding:0 8px; margin-left:6px" onclick="startRtpxDraftReview('${draftId}')">Review</button>
                    <button class="secondary" style="min-height:28px; padding:0 8px; margin-left:6px" onclick="acknowledgeRtpxDraftDiff('${draftId}')">Ack Diff</button>
                    <button class="secondary" style="min-height:28px; padding:0 8px; margin-left:6px" onclick="approveRtpxDraft('${draftId}')">Approve</button>
                    <button class="secondary" style="min-height:28px; padding:0 8px; margin-left:6px" onclick="requestRtpxDraftChanges('${draftId}')">Changes</button>
                    <button class="secondary" style="min-height:28px; padding:0 8px; margin-left:6px" onclick="rejectRtpxDraft('${draftId}')">Reject</button>
                    <button class="secondary" style="min-height:28px; padding:0 8px; margin-left:6px" onclick="promoteRtpxDraft('${draftId}')">Promote</button>
                  </td>
                </tr>`;
              }).join("");
            }

            async function loadProtocolComplianceRuns() {
              const response = await fetch("/api/protocol-compliance/runs?limit=25", { headers: requestHeaders() });
              if (!response.ok) {
                document.getElementById("protocolComplianceRuns").innerHTML = "";
                return;
              }

              const runs = await response.json();
              document.getElementById("protocolComplianceRuns").innerHTML = runs.map(run => {
                const runId = escapeOnclickString(run.id);
                const status = String(run.status || "").toLowerCase();
                return `<tr>
                  <td><code>${escapeHtml(run.id)}</code></td>
                  <td>${escapeHtml(run.planId)}<br><code>${escapeHtml(run.inputKind)}</code></td>
                  <td class="status-${escapeHtml(status)}">${escapeHtml(run.status)}</td>
                  <td>${escapeHtml(run.protocolName)}<br><code>${escapeHtml(run.protocolVersion)}</code></td>
                  <td><code>${escapeHtml(run.rulePackId)}</code><br><code>${escapeHtml(run.versionId)}</code></td>
                  <td>${escapeHtml(run.passCount)} pass / ${escapeHtml(run.warningCount)} warn<br>${escapeHtml(run.failCount + run.notEvaluableCount)} blocking, ${escapeHtml(run.acceptedVarianceCount)} variance</td>
                  <td>${escapeHtml(new Date(run.createdAtUtc).toLocaleString())}</td>
                  <td>
                    <button class="secondary" style="min-height:28px; padding:0 8px" onclick="viewProtocolComplianceRun('${runId}')">View</button>
                    <button class="secondary" style="min-height:28px; padding:0 8px; margin-left:6px" onclick="downloadProtocolComplianceMarkdown('${runId}')">MD</button>
                    <button class="secondary" style="min-height:28px; padding:0 8px; margin-left:6px" onclick="selectProtocolComplianceRun('${runId}')">Variance</button>
                  </td>
                </tr>`;
              }).join("");
            }

            async function viewProtocolComplianceRun(id) {
              const response = await fetch(`/api/protocol-compliance/runs/${id}/report.json`, { headers: requestHeaders() });
              const text = await response.text();
              document.getElementById("output").textContent = text ? JSON.stringify(JSON.parse(text), null, 2) : "{}";
            }

            async function downloadProtocolComplianceMarkdown(id) {
              const response = await fetch(`/api/protocol-compliance/runs/${id}/report.md`, { headers: requestHeaders() });
              if (!response.ok) {
                document.getElementById("output").textContent = await response.text();
                return;
              }

              const blob = new Blob([await response.text()], { type: "text/markdown" });
              const url = URL.createObjectURL(blob);
              const link = document.createElement("a");
              link.href = url;
              link.download = `${id}.beamkit-protocol-compliance.md`;
              link.click();
              URL.revokeObjectURL(url);
            }

            function selectProtocolComplianceRun(id) {
              document.getElementById("protocolComplianceFindingId").dataset.runId = id;
            }

            async function viewRtpxAcceptance(id) {
              await api(`/api/rtpx/acceptance/${id}`, { method: "GET" });
            }

            async function viewRtpxDraft(id) {
              const response = await fetch(`/api/rtpx/drafts/${id}`, { headers: requestHeaders() });
              const text = await response.text();
              const data = text ? JSON.parse(text) : null;
              document.getElementById("output").textContent = JSON.stringify(data, null, 2);
              if (!response.ok) return;
              renderRtpxDraftDiff(data);
            }

            async function startRtpxDraftReview(id) {
              await postRtpxDraftAction(id, "review", "Started review from local BeamKit CI dashboard.");
            }

            async function acknowledgeRtpxDraftDiff(id) {
              await postRtpxDraftAction(id, "acknowledge-diff", "Acknowledged review-relevant protocol diff items.");
            }

            async function approveRtpxDraft(id) {
              await postRtpxDraftAction(id, "approve", "Approved for promotion from local BeamKit CI dashboard.");
            }

            async function requestRtpxDraftChanges(id) {
              await postRtpxDraftAction(id, "request-changes", "Changes requested from local BeamKit CI dashboard.");
            }

            async function rejectRtpxDraft(id) {
              await postRtpxDraftAction(id, "reject", "Rejected from local BeamKit CI dashboard.");
            }

            async function promoteRtpxDraft(id) {
              await postRtpxDraftAction(id, "promote", "Promoted from local BeamKit CI dashboard.");
            }

            async function postRtpxDraftAction(id, action, fallbackNote) {
              const data = await api(`/api/rtpx/drafts/${id}/${action}`, {
                method: "POST",
                body: JSON.stringify({
                  reviewedBy: "dashboard",
                  note: document.getElementById("rtpxDraftReviewNote").value.trim() || fallbackNote
                })
              });
              if (data) renderRtpxDraftDiff(data);
            }

            function renderRtpxDraftDiff(draft) {
              const acceptance = draft?.acceptance || {};
              const changes = draft?.protocolDiff?.changes || [];
              const acknowledged = new Set((draft?.acknowledgedDiffChangeIds || []).map(value => String(value).toLowerCase()));
              document.getElementById("rtpxDraftDiffTitle").textContent = acceptance.id
                ? `Selected Draft Diff: ${acceptance.id}`
                : "Selected Draft Diff";
              if (changes.length === 0) {
                document.getElementById("rtpxDraftDiffs").innerHTML = "<tr><td colspan=\"5\">No protocol changes were detected.</td></tr>";
                return;
              }

              document.getElementById("rtpxDraftDiffs").innerHTML = changes.map(change => {
                const id = String(change.id || `${change.category}:${change.key}:${change.changeType}`).toLowerCase();
                const needsAck = String(change.severity || "").toLowerCase() !== "info";
                const status = !needsAck ? "info" : acknowledged.has(id) ? "acknowledged" : "pending";
                return `<tr>
                  <td>${escapeHtml(change.category)} / ${escapeHtml(change.changeType)}<br><code>${escapeHtml(change.key)}</code></td>
                  <td>${escapeHtml(change.severity || "")}</td>
                  <td>${escapeHtml(status)}</td>
                  <td>${escapeHtml(change.before || "")}</td>
                  <td>${escapeHtml(change.after || "")}</td>
                </tr>`;
              }).join("");
            }

            function selectWorkItem(id) {
              document.getElementById("workItemId").value = id;
            }

            function formatInputKind(inputKind) {
              switch (inputKind) {
                case "BeamKitPlanJson": return "BeamKit Plan JSON";
                case "EsapiSnapshotJson": return "ESAPI Snapshot";
                default: return "Synthetic";
              }
            }

            function formatDraftStatus(status) {
              switch (status) {
                case "InReview": return "In review";
                case "ChangesRequested": return "Changes requested";
                default: return status || "Draft";
              }
            }

            loadRuns();
            loadRtpxAcceptances();
            loadRtpxDrafts();
            loadProtocolComplianceRuns();
            loadWorkItems();
          </script>
        </body>
        </html>
        """;
}
