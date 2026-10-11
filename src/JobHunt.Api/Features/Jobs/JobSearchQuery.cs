namespace JobHunt.Api.Features.Jobs;

public sealed record JobSearchQuery
{
    public IReadOnlyList<string> Keywords { get; init; } = [];

    public IReadOnlyList<string> Locations { get; init; } = [];

    public IReadOnlyList<string> WorkModels { get; init; } = [];

    public int? SalaryMin { get; init; }

    public int? SalaryMax { get; init; }

    public int MaxResults { get; init; } = 25;
}
