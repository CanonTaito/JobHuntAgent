using JobHunt.Api.Ai;
using JobHunt.Api.Features.Applications;
using JobHunt.Api.Features.Approvals;
using JobHunt.Api.Features.Discovery;
using JobHunt.Api.Features.FollowUps;
using JobHunt.Api.Features.JdScan;
using JobHunt.Api.Features.Jobs;
using JobHunt.Api.Features.Match;
using JobHunt.Api.Features.Observability;
using JobHunt.Api.Features.Outcomes;
using JobHunt.Api.Features.Persistence;
using JobHunt.Api.Features.Pii;
using JobHunt.Api.Features.Profile;
using JobHunt.Api.Features.Ranking;
using JobHunt.Api.Features.Resume;
using JobHunt.Api.Features.Salary;
using JobHunt.Api.Features.Tailoring;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

DotEnv.LoadInto(builder.Configuration);
builder.Services.AddJobHuntAi(builder.Configuration);
builder.Services.AddCandidateProfile(builder.Configuration, builder.Environment);
builder.Services.AddProfileStore();
builder.Services.AddPiiProtection();
builder.Services.AddResumeParsing();
builder.Services.AddJobSources(builder.Configuration);
builder.Services.AddJobHuntDatabase(builder.Configuration, builder.Environment);
builder.Services.AddSingleton<JdScannerAgent>();
builder.Services.AddSingleton<MatchTools>();
builder.Services.AddSingleton<IMatchAgent>(sp => MatchAgentFactory.Create(
    sp.GetRequiredService<AiOptions>(),
    sp.GetRequiredService<IChatClient>(),
    sp.GetRequiredService<MatchTools>(),
    sp.GetRequiredService<ILoggerFactory>()));
builder.Services.AddSingleton<EmbeddingScorer>();
builder.Services.AddSingleton<MatchScorer>();
builder.Services.AddSingleton<IMatchScorer>(sp => sp.GetRequiredService<MatchScorer>());
builder.Services.AddSingleton<SalaryBandInference>();
builder.Services.AddSingleton<SalaryInferenceValidator>();
builder.Services.AddSingleton<JobDiscoveryPipeline>();
builder.Services.AddSingleton<TailoringAgent>();
builder.Services.AddSingleton<TailoringService>();
builder.Services.AddSingleton<ApplicationQueries>();
builder.Services.AddSingleton<ApprovalService>();
builder.Services.AddSingleton<OutcomeStore>();
builder.Services.AddSingleton<OutcomeInsightsService>();
builder.Services.AddSingleton<MatchReRanker>();
builder.Services.AddSingleton<FollowUpAgent>();
builder.Services.AddSingleton<FollowUpService>();
builder.Services.AddSingleton<JobHuntMetrics>();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(JobHuntTelemetry.ServiceName))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(options =>
            options.Filter = context => !context.Request.Path.StartsWithSegments("/health"))
        .AddHttpClientInstrumentation(options => options.RecordException = true)
        .AddEntityFrameworkCoreInstrumentation()
        .AddSource(JobHuntTelemetry.ActivitySourceName))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter(JobHuntTelemetry.MeterName))
    .WithLogging()
    .UseOtlpExporter();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<JobHuntDbContext>("database", tags: ["ready"]);

var app = builder.Build();

var contextFactory = app.Services.GetRequiredService<IDbContextFactory<JobHuntDbContext>>();
await using (var db = contextFactory.CreateDbContext())
{
    db.Database.Migrate();
}

app.MapGet("/", () => "Hello World!");

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.MapGet("/api/ai/ping", async (IChatClient chat, CancellationToken ct) =>
{
    var response = await chat.GetResponseAsync("Reply with exactly: pong", options: null, ct);
    return Results.Ok(new { message = response.Text.Trim() });
});

app.MapGet("/api/profile", (IProfileStore profiles) => Results.Ok(profiles.Current));

app.MapGet("/api/profile/redacted", (ProfileRedactorProvider redactors) =>
    Results.Ok(redactors.RedactCurrent()));

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

app.MapPost("/api/resume", async (HttpRequest request, ResumeTextExtractor extractor, ResumeParserAgent parser, IProfileStore profiles, CancellationToken ct) =>
{
    if (!request.HasFormContentType)
    {
        return Results.BadRequest(new { error = "Request must be multipart/form-data with a resume file and a candidate name." });
    }

    var form = await request.ReadFormAsync(ct);

    var name = form["name"].ToString().Trim();
    if (string.IsNullOrWhiteSpace(name))
    {
        return Results.BadRequest(new { error = "A candidate name is required." });
    }

    var file = form.Files["file"] ?? form.Files.FirstOrDefault();
    if (file is null || file.Length == 0)
    {
        return Results.BadRequest(new { error = "A resume file is required." });
    }

    if (file.Length > 10 * 1024 * 1024)
    {
        return Results.BadRequest(new { error = "The resume file must be 10 MB or smaller." });
    }

    try
    {
        await using var stream = file.OpenReadStream();
        var text = await extractor.ExtractAsync(stream, file.FileName, ct);
        var profile = await parser.ParseAsync(text, name, ct);
        await profiles.SaveAsync(profile, ct);
        return Results.Ok(profile);
    }
    catch (NotSupportedException e)
    {
        return Results.BadRequest(new { error = e.Message });
    }
    catch (InvalidDataException e)
    {
        return Results.BadRequest(new { error = e.Message });
    }
    catch (ResumeParseFormatException e)
    {
        return Results.Problem(e.Message, statusCode: StatusCodes.Status502BadGateway);
    }
}).DisableAntiforgery();

app.MapPost("/api/discover", async (JobDiscoveryPipeline pipeline, int? max, CancellationToken ct) =>
    Results.Ok(await pipeline.RunAsync(max ?? 5, ct)));

app.MapGet("/api/salary/validate", async (SalaryInferenceValidator validator, int? sample, CancellationToken ct) =>
    Results.Ok(await validator.ValidateAsync(sample ?? 10, ct)));

app.MapPost("/api/applications/{id:int}/tailor", async (int id, TailoringService tailoring, CancellationToken ct) =>
{
    var draft = await tailoring.TailorAsync(id, ct);
    return draft is null ? Results.NotFound() : Results.Ok(draft);
});

app.MapGet("/api/applications/ranking-insights", async (OutcomeInsightsService insights, CancellationToken ct) =>
    Results.Ok(await insights.BuildAsync(ct)));

app.MapGet("/api/applications", async (ApplicationQueries queries, string? status, CancellationToken ct) =>
{
    JobApplicationStatus? parsed = null;
    if (!string.IsNullOrWhiteSpace(status))
    {
        if (!Enum.TryParse<JobApplicationStatus>(status, ignoreCase: true, out var value))
        {
            return Results.BadRequest(new { error = $"Unknown status '{status}'." });
        }

        parsed = value;
    }

    return Results.Ok(await queries.ListAsync(parsed, ct));
});

app.MapGet("/api/applications/{id:int}", async (int id, ApplicationQueries queries, CancellationToken ct) =>
{
    var application = await queries.GetAsync(id, ct);
    return application is null ? Results.NotFound() : Results.Ok(application);
});

app.MapPost("/api/applications/{id:int}/request-approval", async (int id, ApprovalService approvals, CancellationToken ct) =>
    ToResult(await approvals.RequestApprovalAsync(id, ct)));

app.MapPost("/api/applications/{id:int}/approve", async (int id, ApprovalService approvals, CancellationToken ct) =>
    ToResult(await approvals.ApproveAsync(id, ct)));

app.MapPost("/api/applications/{id:int}/reject", async (int id, ApprovalService approvals, CancellationToken ct) =>
    ToResult(await approvals.RejectAsync(id, ct)));

app.MapPost("/api/applications/{id:int}/outcome", async (int id, OutcomeRequest request, OutcomeStore outcomes, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Outcome) ||
        !Enum.TryParse<ApplicationOutcome>(request.Outcome, ignoreCase: true, out var outcome))
    {
        return Results.BadRequest(new { error = $"Unknown outcome '{request.Outcome}'." });
    }

    return ToOutcomeResult(await outcomes.RecordAsync(id, outcome, request.Note, ct));
});

app.MapGet("/api/applications/{id:int}/outcomes", async (int id, OutcomeStore outcomes, CancellationToken ct) =>
{
    var records = await outcomes.ListAsync(id, ct);
    return records is null ? Results.NotFound() : Results.Ok(records);
});

app.MapPost("/api/applications/{id:int}/follow-up", async (int id, FollowUpService followUps, CancellationToken ct) =>
    ToFollowUpResult(await followUps.DraftAsync(id, ct)));

app.Run();

static IResult ToResult(ApplicationTransitionResult result) => result.Outcome switch
{
    TransitionOutcome.Succeeded => Results.Ok(result.Application),
    TransitionOutcome.NotFound => Results.NotFound(),
    _ => Results.Conflict(new { error = result.Message }),
};

static IResult ToOutcomeResult(OutcomeResult result) => result.Status switch
{
    OutcomeResultStatus.Succeeded => Results.Ok(result.Record),
    OutcomeResultStatus.NotFound => Results.NotFound(),
    _ => Results.Conflict(new { error = result.Message }),
};

static IResult ToFollowUpResult(FollowUpResult result) => result.Status switch
{
    FollowUpResultStatus.Succeeded => Results.Ok(result.Draft),
    FollowUpResultStatus.NotFound => Results.NotFound(),
    _ => Results.Conflict(new { error = result.Message }),
};