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

            input, select {
              width: 100%;
              min-height: 36px;
              border: 1px solid var(--line);
              border-radius: 6px;
              padding: 0 10px;
              color: var(--text);
              background: white;
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
                <h2>Assignment</h2>
                <div class="stack" style="margin-top:12px">
                  <label>Disease site
                    <input id="diseaseSite" value="Head and Neck">
                  </label>
                  <label>Required skill
                    <input id="requiredSkill" value="VMAT">
                  </label>
                </div>
                <div class="actions">
                  <button onclick="recommendAssignment()">Recommend</button>
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
              <h2 style="margin-top:22px">Response</h2>
              <pre id="output">{}</pre>
            </section>
          </main>
          <script>
            async function api(path, options) {
              const response = await fetch(path, {
                headers: { "content-type": "application/json" },
                ...options
              });
              const text = await response.text();
              const data = text ? JSON.parse(text) : null;
              document.getElementById("output").textContent = JSON.stringify(data, null, 2);
              await loadRuns();
              return data;
            }

            async function createRun() {
              await api("/api/runs", {
                method: "POST",
                body: JSON.stringify({
                  syntheticCaseId: document.getElementById("caseId").value,
                  branch: document.getElementById("branch").value,
                  commit: document.getElementById("commit").value,
                  buildId: document.getElementById("buildId").value
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

            async function recommendAssignment() {
              await api("/api/assignments/recommend", {
                method: "POST",
                body: JSON.stringify({
                  diseaseSite: document.getElementById("diseaseSite").value,
                  requiredSkills: [document.getElementById("requiredSkill").value]
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
              const response = await fetch(`/api/runs?${params}`);
              const runs = await response.json();
              document.getElementById("runs").innerHTML = runs.map(run => {
                const status = String(run.status).toLowerCase();
                return `<tr>
                  <td><code>${run.id}</code></td>
                  <td>${formatInputKind(run.inputKind)}</td>
                  <td>${run.caseId || run.syntheticCaseId}</td>
                  <td class="status-${status}">${run.status}</td>
                  <td>${run.hasPlanSnapshot ? "Plan" : "Metadata"}</td>
                  <td>${run.exitCode}</td>
                  <td>${new Date(run.createdAtUtc).toLocaleString()}</td>
                  <td>
                    <a href="/api/runs/${run.id}/artifact/download">JSON</a>
                    <button class="secondary" style="min-height:28px; padding:0 8px; margin-left:6px" onclick="promoteBaseline('${run.id}')">Baseline</button>
                    <button class="secondary" style="min-height:28px; padding:0 8px; margin-left:6px" onclick="compareBaseline('${run.id}')">Compare</button>
                  </td>
                </tr>`;
              }).join("");
            }

            function formatInputKind(inputKind) {
              switch (inputKind) {
                case "BeamKitPlanJson": return "BeamKit Plan JSON";
                case "EsapiSnapshotJson": return "ESAPI Snapshot";
                default: return "Synthetic";
              }
            }

            loadRuns();
          </script>
        </body>
        </html>
        """;
}
