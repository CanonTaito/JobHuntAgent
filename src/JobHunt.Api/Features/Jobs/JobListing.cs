namespace JobHunt.Api.Features.Jobs;

public sealed record JobListing
{
    public string Id { get; init; } = string.Empty;

    public string Source { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Company { get; init; } = string.Empty;

    public string? Location { get; init; }

    public string? WorkModel { get; init; }

    public string Description { get; init; } = string.Empty;

    public int? SalaryMin { get; init; }

    public int? SalaryMax { get; init; }

    public string? SalaryCurrency { get; init; }

    public string? SalaryText { get; init; }

    public string? Url { get; init; }

    public DateOnly? PostedAt { get; init; }
}
