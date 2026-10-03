using JobHunt.Api.Ai;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

DotEnv.LoadInto(builder.Configuration);
builder.Services.AddJobHuntAi(builder.Configuration);

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapGet("/api/ai/ping", async (IChatClient chat, CancellationToken ct) =>
{
    var response = await chat.GetResponseAsync("Reply with exactly: pong", options: null, ct);
    return Results.Ok(new { message = response.Text.Trim() });
});

app.Run();