using System.Text;
using System.Text.Json.Serialization;
using BeamKit.CiServer;
using BeamKit.Sdk;
using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.WriteIndented = true;
});
builder.Services.Configure<CiServerStorageOptions>(builder.Configuration.GetSection("BeamKit:CiServer:Storage"));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<BeamKitClient>();
builder.Services.AddSingleton<ICiRunStore, SqliteCiRunStore>();
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
app.MapGet("/api/runs/{id}/artifact/download", (string id, BeamKitCiServerService service) =>
{
    var artifactJson = service.FindArtifactJson(id);
    if (artifactJson is null)
    {
        return Results.NotFound();
    }

    return Results.File(
        Encoding.UTF8.GetBytes(artifactJson),
        "application/json",
        $"{id}.beamkit-ci-artifact.json");
});
app.MapGet("/api/runs/{id}/baseline-comparison", (string id, BeamKitCiServerService service) =>
{
    return Results.Ok(service.CompareToBaseline(id));
});
app.MapPost("/api/runs", (HostedCiRunRequest request, BeamKitCiServerService service) =>
{
    var record = service.CreateRun(request);
    return Results.Created($"/api/runs/{record.Id}", record);
});
app.MapPost("/api/runs/{id}/baseline", (string id, PromoteCiRunBaselineRequest request, BeamKitCiServerService service) =>
{
    var baseline = service.PromoteBaseline(id, request);
    return Results.Created($"/api/baselines/{baseline.CaseId}", baseline);
});
app.MapPost("/api/runs/from-plan-snapshot", (HostedCiRunUploadRequest request, BeamKitCiServerService service) =>
{
    var record = service.CreateRunFromPlanSnapshot(request);
    return Results.Created($"/api/runs/{record.Id}", record);
});
app.MapGet("/api/baselines", (BeamKitCiServerService service) => Results.Ok(service.ListBaselines()));
app.MapGet("/api/baselines/{caseId}", (string caseId, BeamKitCiServerService service) =>
{
    var baseline = service.FindBaseline(caseId);
    return baseline is null ? Results.NotFound() : Results.Ok(baseline);
});
app.MapPost("/api/rule-packs/validate", (RulePackValidationServerRequest request, BeamKitCiServerService service) =>
{
    return Results.Ok(service.ValidateRulePack(request));
});
app.MapPost("/api/rule-packs/test", (RulePackTestServerRequest request, BeamKitCiServerService service) =>
{
    return Results.Ok(service.TestRulePack(request));
});
app.MapPost("/api/assignments/recommend", (AssignmentServerRequest request, BeamKitCiServerService service) =>
{
    return Results.Ok(service.RecommendAssignment(request));
});

app.Run();
