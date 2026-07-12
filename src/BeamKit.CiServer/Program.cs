using System.Text;
using System.Text.Json.Serialization;
using BeamKit.CiServer;
using BeamKit.Safety;
using BeamKit.Sdk;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.WriteIndented = true;
});
builder.Services.Configure<CiServerStorageOptions>(builder.Configuration.GetSection("BeamKit:CiServer:Storage"));
builder.Services.Configure<CiServerSecurityOptions>(builder.Configuration.GetSection("BeamKit:CiServer:Security"));
builder.Services.Configure<CiServerRulePackRegistryOptions>(builder.Configuration.GetSection("BeamKit:CiServer:RulePackRegistry"));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<BeamKitClient>();
builder.Services.AddSingleton<ICiRunStore, SqliteCiRunStore>();
builder.Services.AddSingleton<CiServerRulePackRegistry>();
builder.Services.AddSingleton<BeamKitCiServerService>();

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var statusCode = exception is ArgumentException or InvalidOperationException or FormatException ? StatusCodes.Status400BadRequest : StatusCodes.Status500InternalServerError;
        context.Response.StatusCode = statusCode;
        await Results.Problem(
            title: statusCode == StatusCodes.Status400BadRequest ? "Invalid BeamKit CI server request." : "BeamKit CI server error.",
            detail: exception?.Message,
            statusCode: statusCode)
            .ExecuteAsync(context);
    });
});

app.Use(async (context, next) =>
{
    var security = context.RequestServices.GetRequiredService<IOptions<CiServerSecurityOptions>>().Value;
    if (CiServerSecurity.IsPlanSnapshotUploadPath(context.Request.Path))
    {
        var maxBytes = security.ClampedMaxPlanSnapshotUploadBytes;
        var maxBodyFeature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (maxBodyFeature is { IsReadOnly: false })
        {
            maxBodyFeature.MaxRequestBodySize = maxBytes;
        }

        if (context.Request.ContentLength > maxBytes)
        {
            await CiServerSecurity.PayloadTooLarge(maxBytes).ExecuteAsync(context);
            return;
        }
    }

    if (!CiServerSecurity.TryAuthenticate(context, security, out var failure))
    {
        await failure!.ExecuteAsync(context);
        return;
    }

    await next(context);
});

app.MapGet("/", () => Results.Content(DashboardHtml.Content, "text/html"));
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "BeamKit.CiServer" }));
app.MapGet("/api/cases", (BeamKitCiServerService service) => Results.Ok(service.ListCases()));
app.MapGet("/api/runs", (
    BeamKitCiServerService service,
    int? limit,
    string? status,
    string? caseId,
    string? syntheticCaseId,
    string? branch,
    string? createdFrom,
    string? createdTo) =>
{
    var query = CiRunQueryParser.Parse(limit, status, syntheticCaseId ?? caseId, branch, createdFrom, createdTo);
    return Results.Ok(service.ListRuns(query));
});
app.MapGet("/api/runs/{id}", (string id, BeamKitCiServerService service) =>
{
    var record = service.FindRun(id);
    return record is null ? Results.NotFound() : Results.Ok(record);
});
app.MapGet("/api/runs/{id}/artifact", (string id, BeamKitCiServerService service) =>
{
    var artifactJson = service.FindArtifactJson(id);
    return artifactJson is null ? Results.NotFound() : Results.Text(artifactJson, "application/json");
});
app.MapGet("/api/runs/{id}/artifact/download", (string id, HttpContext context, BeamKitCiServerService service) =>
{
    var artifactJson = service.FindArtifactJson(id);
    if (artifactJson is null)
    {
        return Results.NotFound();
    }

    service.RecordArtifactDownloaded(id, CiServerAuditContext.FromHttpContext(context));
    return Results.File(
        Encoding.UTF8.GetBytes(artifactJson),
        "application/json",
        $"{id}.beamkit-ci-artifact.json");
});
app.MapGet("/api/runs/{id}/baseline-comparison", (string id, HttpContext context, BeamKitCiServerService service) =>
{
    return Results.Ok(service.CompareToBaseline(id, CiServerAuditContext.FromHttpContext(context)));
});
app.MapPost("/api/runs", (HostedCiRunRequest request, HttpContext context, BeamKitCiServerService service) =>
{
    var record = service.CreateRun(request, CiServerAuditContext.FromHttpContext(context));
    return Results.Created($"/api/runs/{record.Id}", record);
});
app.MapPost("/api/runs/{id}/baseline", (string id, PromoteCiRunBaselineRequest request, HttpContext context, BeamKitCiServerService service) =>
{
    var baseline = service.PromoteBaseline(id, request, CiServerAuditContext.FromHttpContext(context));
    return Results.Created($"/api/baselines/{baseline.CaseId}", baseline);
});
app.MapPost("/api/runs/from-plan-snapshot", (HostedCiRunUploadRequest request, HttpContext context, BeamKitCiServerService service) =>
{
    var record = service.CreateRunFromPlanSnapshot(request, CiServerAuditContext.FromHttpContext(context));
    return Results.Created($"/api/runs/{record.Id}", record);
});
app.MapGet("/api/baselines", (BeamKitCiServerService service) => Results.Ok(service.ListBaselines()));
app.MapGet("/api/baselines/{caseId}", (string caseId, BeamKitCiServerService service) =>
{
    var baseline = service.FindBaseline(caseId);
    return baseline is null ? Results.NotFound() : Results.Ok(baseline);
});
app.MapGet("/api/rule-packs", (BeamKitCiServerService service) => Results.Ok(service.ListRulePacks()));
app.MapGet("/api/rule-packs/versions", (BeamKitCiServerService service, string? rulePackId) =>
{
    return Results.Ok(service.ListManagedRulePackVersions(rulePackId));
});
app.MapPost("/api/rule-packs/import", (RulePackImportServerRequest request, HttpContext context, BeamKitCiServerService service) =>
{
    var result = service.ImportRulePack(request, CiServerAuditContext.FromHttpContext(context));
    return Results.Created($"/api/rule-packs/{result.Version.RulePackId}/versions/{result.Version.VersionId}", result);
});
app.MapGet("/api/rule-packs/{id}/versions", (string id, BeamKitCiServerService service) =>
{
    return Results.Ok(service.ListManagedRulePackVersions(id));
});
app.MapGet("/api/rule-packs/{id}/versions/{versionId}", (string id, string versionId, BeamKitCiServerService service) =>
{
    var version = service.FindManagedRulePackVersion(id, versionId);
    return version is null ? Results.NotFound() : Results.Ok(version);
});
app.MapGet("/api/rule-packs/{id}/versions/{versionId}/safety-evidence", (string id, string versionId, BeamKitCiServerService service) =>
{
    var evidence = service.FindManagedRulePackSafetyEvidence(id, versionId);
    return evidence is null ? Results.NotFound() : Results.Ok(evidence);
});
app.MapPost("/api/rule-packs/{id}/versions/{versionId}/safety-evidence/validate", (string id, string versionId, ValidationEvidencePackage evidence, HttpContext context, BeamKitCiServerService service) =>
{
    return Results.Ok(service.ReviewManagedRulePackSafetyEvidence(id, versionId, evidence, CiServerAuditContext.FromHttpContext(context)));
});
app.MapGet("/api/rule-packs/{id}/versions/{oldVersionId}/diff/{newVersionId}", (string id, string oldVersionId, string newVersionId, HttpContext context, BeamKitCiServerService service) =>
{
    return Results.Ok(service.CompareManagedRulePackVersions(id, oldVersionId, newVersionId, CiServerAuditContext.FromHttpContext(context)));
});
app.MapPost("/api/rule-packs/{id}/review-draft", (string id, RulePackImportServerRequest request, HttpContext context, BeamKitCiServerService service) =>
{
    return Results.Ok(service.ReviewRulePackDraft(id, request, CiServerAuditContext.FromHttpContext(context)));
});
app.MapPost("/api/rule-packs/{id}/versions/{versionId}/validate", (string id, string versionId, HttpContext context, BeamKitCiServerService service) =>
{
    return Results.Ok(service.ValidateManagedRulePackVersion(id, versionId, CiServerAuditContext.FromHttpContext(context)));
});
app.MapPost("/api/rule-packs/{id}/versions/{versionId}/test", (string id, string versionId, RulePackVersionTestServerRequest request, HttpContext context, BeamKitCiServerService service) =>
{
    return Results.Ok(service.TestManagedRulePackVersion(id, versionId, request, CiServerAuditContext.FromHttpContext(context)));
});
app.MapPost("/api/rule-packs/{id}/versions/{versionId}/promote", (string id, string versionId, RulePackPromotionServerRequest request, HttpContext context, BeamKitCiServerService service) =>
{
    return Results.Ok(service.PromoteManagedRulePackVersion(id, versionId, request, CiServerAuditContext.FromHttpContext(context)));
});
app.MapGet("/api/rule-packs/{id}", (string id, BeamKitCiServerService service) =>
{
    var rulePack = service.FindRulePack(id);
    return rulePack is null ? Results.NotFound() : Results.Ok(rulePack);
});
app.MapPost("/api/rule-packs/validate", (RulePackValidationServerRequest request, HttpContext context, BeamKitCiServerService service) =>
{
    return Results.Ok(service.ValidateRulePack(request, CiServerAuditContext.FromHttpContext(context)));
});
app.MapPost("/api/rule-packs/test", (RulePackTestServerRequest request, HttpContext context, BeamKitCiServerService service) =>
{
    return Results.Ok(service.TestRulePack(request, CiServerAuditContext.FromHttpContext(context)));
});
app.MapPost("/api/rule-packs/{id}/validate", (string id, HttpContext context, BeamKitCiServerService service) =>
{
    return Results.Ok(service.ValidateRulePack(id, CiServerAuditContext.FromHttpContext(context)));
});
app.MapPost("/api/rule-packs/{id}/test", (string id, RulePackTestServerRequest request, HttpContext context, BeamKitCiServerService service) =>
{
    return Results.Ok(service.TestRulePack(id, request, CiServerAuditContext.FromHttpContext(context)));
});
app.MapPost("/api/assignments/recommend", (AssignmentServerRequest request, HttpContext context, BeamKitCiServerService service) =>
{
    return Results.Ok(service.RecommendAssignment(request, CiServerAuditContext.FromHttpContext(context)));
});
app.MapPost("/api/assignments/recommend-team", (AssignmentServerRequest request, HttpContext context, BeamKitCiServerService service) =>
{
    return Results.Ok(service.RecommendStaffing(request, CiServerAuditContext.FromHttpContext(context)));
});
app.MapGet("/api/work-items", (
    BeamKitCiServerService service,
    int? limit,
    string? status,
    string? caseId,
    string? diseaseSite,
    string? assignedStaffId,
    bool? activeOnly) =>
{
    var query = CaseWorkItemQueryParser.Parse(limit, status, caseId, diseaseSite, assignedStaffId, activeOnly);
    return Results.Ok(service.ListWorkItems(query));
});
app.MapGet("/api/work-items/{id}", (string id, BeamKitCiServerService service) =>
{
    var workItem = service.FindWorkItem(id);
    return workItem is null ? Results.NotFound() : Results.Ok(workItem);
});
app.MapPost("/api/work-items", (CreateCaseWorkItemRequest request, HttpContext context, BeamKitCiServerService service) =>
{
    var workItem = service.CreateWorkItem(request, CiServerAuditContext.FromHttpContext(context));
    return Results.Created($"/api/work-items/{workItem.Id}", workItem);
});
app.MapPost("/api/work-items/{id}/recommend-assignment", (string id, AssignmentServerRequest request, HttpContext context, BeamKitCiServerService service) =>
{
    return Results.Ok(service.RecommendWorkItemAssignment(id, request, CiServerAuditContext.FromHttpContext(context)));
});
app.MapPost("/api/work-items/{id}/assign", (string id, AssignCaseWorkItemRequest request, HttpContext context, BeamKitCiServerService service) =>
{
    return Results.Ok(service.AssignWorkItem(id, request, CiServerAuditContext.FromHttpContext(context)));
});
app.MapPost("/api/work-items/{id}/status", (string id, UpdateCaseWorkItemStatusRequest request, HttpContext context, BeamKitCiServerService service) =>
{
    return Results.Ok(service.UpdateWorkItemStatus(id, request, CiServerAuditContext.FromHttpContext(context)));
});
app.MapGet("/api/audit-events", (
    BeamKitCiServerService service,
    int? limit,
    string? action,
    string? runId,
    string? caseId) =>
{
    return Results.Ok(service.ListAuditEvents(new CiServerAuditQuery
    {
        Limit = limit ?? 100,
        Action = action,
        RunId = runId,
        CaseId = caseId
    }));
});

app.Run();

/// <summary>
/// ASP.NET Core entrypoint marker used by integration tests.
/// </summary>
public partial class Program;
