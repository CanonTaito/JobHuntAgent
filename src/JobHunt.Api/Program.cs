using JobHunt.Api.Ai;
using JobHunt.Api.Features.JdScan;
using JobHunt.Api.Features.Pii;
using JobHunt.Api.Features.Profile;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

DotEnv.LoadInto(builder.Configuration);
builder.Services.AddJobHuntAi(builder.Configuration);
builder.Services.AddCandidateProfile(builder.Configuration, builder.Environment);
builder.Services.AddPiiProtection();
builder.Services.AddSingleton<JdScannerAgent>();

var app = builder.Build();

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

app.Run();