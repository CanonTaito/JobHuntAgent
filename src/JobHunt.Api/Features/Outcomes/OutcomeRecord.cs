namespace JobHunt.Api.Features.Outcomes;

public sealed class OutcomeRecord
{
    public int Id { get; set; }

    public int JobApplicationId { get; set; }

    public ApplicationOutcome Outcome { get; set; }

    public string? Note { get; set; }

    public DateTimeOffset RecordedAt { get; set; }
}
