using JobHunt.Api.Ai;
using JobHunt.Api.Features.Approvals;
using JobHunt.Api.Features.Discovery;
using JobHunt.Api.Features.JdScan;
using JobHunt.Api.Features.Jobs;
using JobHunt.Api.Features.Match;
using JobHunt.Api.Features.Persistence;
using JobHunt.Api.Features.Pii;
using JobHunt.Api.Features.Profile;
using JobHunt.Api.Features.Salary;
using JobHunt.Api.Features.Tailoring;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

DotEnv.LoadInto(builder.Configuration);
builder.Services.AddJobHuntAi(builder.Configuration);
builder.Services.AddCandidateProfile(builder.Configuration, builder.Environment);
builder.Services.AddPiiProtection();
builder.Services.AddJobSources();
builder.Services.AddJobHuntDatabase(builder.Configuration, builder.Environment);
builder.Services.AddSingleton<JdScannerAgent>();
builder.Services.AddSingleton<MatchTools>();
builder.Services.AddSingleton<MatchAgent>();
builder.Services.AddSingleton<EmbeddingScorer>();
builder.Services.AddSingleton<MatchScorer>();
builder.Services.AddSingleton<SalaryBandInference>();
builder.Services.AddSingleton<JobDiscoveryPipeline>();
builder.Services.AddSingleton<TailoringAgent>();
builder.Services.AddSingleton<TailoringService>();
builder.Services.AddSingleton<ApprovalService>();

var app = builder.Build();

var contextFactory = app.Services.GetRequiredService<IDbContextFactory<JobHuntDbContext>>();
await using (var db = contextFactory.CreateDbContext())
{
    db.Database.Migrate();
}

app.MapGet("/", () => "Hello World!");

app.MapGet("/api/ai/ping", async (IChatClient chat, CancellationToken ct) =>
{
    var response = await chat.GetResponseAsync("Reply with exactly: pong", options: null, ct);
    return Results.Ok(new { message = response.Text.Trim() });
});

app.MapGet("/api/profile", (CandidateProfile profile) => Results.Ok(profile));

app.MapGet("/api/profile/redacted", (CandidateProfile profile, PiiRedactor redactor) =>
    Results.Ok(redactor.Redact(profile)));

app.MapPost("/api/scan", async (JdScannerAgent scanner, HttpRequest request, CancellationToken ct) =>
{
    var jd = await new StreamReader(request.Body).ReadToEndAsync(ct);
    if (string.IsNullOrWhiteSpace(jd))
    {
        return Results.BadRequest(new { error = "Request body must contain the job description text." });
    }

    return Results.Ok(await scanner.ScanAsync(jd, ct));
});

app.MapPost("/api/match", async (MatchScorer scorer, HttpRequest request, CancellationToken ct) =>
{
    var jd = await new StreamReader(request.Body).ReadToEndAsync(ct);
    if (string.IsNullOrWhiteSpace(jd))
    {
        return Results.BadRequest(new { error = "Request body must contain the job description text." });
    }

    return Results.Ok(await scorer.ScoreAsync(jd, ct));
});

app.MapPost("/api/discover", async (JobDiscoveryPipeline pipeline, int? max, CancellationToken ct) =>
    Results.Ok(await pipeline.RunAsync(max ?? 5, ct)));

app.MapPost("/api/applications/{id:int}/tailor", async (int id, TailoringService tailoring, CancellationToken ct) =>
{
    var draft = await tailoring.TailorAsync(id, ct);
    return draft is null ? Results.NotFound() : Results.Ok(draft);
});

app.MapGet("/api/applications/{id:int}", async (int id, ApprovalService approvals, CancellationToken ct) =>
{
    var application = await approvals.GetAsync(id, ct);
    return application is null ? Results.NotFound() : Results.Ok(application);
});

app.MapPost("/api/applications/{id:int}/request-approval", async (int id, ApprovalService approvals, CancellationToken ct) =>
    ToResult(await approvals.RequestApprovalAsync(id, ct)));

app.MapPost("/api/applications/{id:int}/approve", async (int id, ApprovalService approvals, CancellationToken ct) =>
    ToResult(await approvals.ApproveAsync(id, ct)));

app.MapPost("/api/applications/{id:int}/reject", async (int id, ApprovalService approvals, CancellationToken ct) =>
    ToResult(await approvals.RejectAsync(id, ct)));

app.Run();

static IResult ToResult(ApplicationTransitionResult result) => result.Outcome switch
{
    TransitionOutcome.Succeeded => Results.Ok(result.Application),
    TransitionOutcome.NotFound => Results.NotFound(),
    _ => Results.Conflict(new { error = result.Message }),
};