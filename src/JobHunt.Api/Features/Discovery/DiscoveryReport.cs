namespace JobHunt.Api.Features.Discovery;

public sealed record DiscoveryReport
{
    public int Discovered { get; init; }

    public int Scored { get; init; }

    public int Skipped { get; init; }

    public int Failed { get; init; }

    public IReadOnlyList<DiscoveryItem> Items { get; init; } = [];
}

public sealed record DiscoveryItem
{
    public int ApplicationId { get; init; }

    public string SourceId { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Company { get; init; } = string.Empty;

    public int Overall { get; init; }

    public string? SalaryBand { get; init; }

    public string Status { get; init; } = string.Empty;
}
