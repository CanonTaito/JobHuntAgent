namespace JobHunt.Api.Features.Outcomes;

public sealed record OutcomeView
{
    public int Id { get; init; }

    public int ApplicationId { get; init; }

    public string Outcome { get; init; } = string.Empty;

    public string? Note { get; init; }

    public DateTimeOffset RecordedAt { get; init; }

    public static OutcomeView From(OutcomeRecord record) => new()
    {
        Id = record.Id,
        ApplicationId = record.JobApplicationId,
        Outcome = record.Outcome.ToString(),
        Note = record.Note,
        RecordedAt = record.RecordedAt,
    };
}
