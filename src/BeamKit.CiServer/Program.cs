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
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<BeamKitClient>();
builder.Services.AddSingleton<CiRunStore>();
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
app.MapGet("/api/runs", (BeamKitCiServerService service, int? limit) => Results.Ok(service.ListRuns(limit ?? 50)));
app.MapGet("/api/runs/{id}", (string id, BeamKitCiServerService service) =>
{
    var record = service.FindRun(id);
    return record is null ? Results.NotFound() : Results.Ok(record);
});
app.MapGet("/api/runs/{id}/artifact", (string id, BeamKitCiServerService service) =>
{
    var record = service.FindRun(id);
    return record is null ? Results.NotFound() : Results.Ok(record.Artifact);
});
app.MapPost("/api/runs", (HostedCiRunRequest request, BeamKitCiServerService service) =>
{
    var record = service.CreateRun(request);
    return Results.Created($"/api/runs/{record.Id}", record);
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
