namespace JobHunt.Api.Features.JdScan;

public sealed record JdAnalysis
{
    public string Title { get; init; } = string.Empty;
    public string? Company { get; init; }
    public string? Location { get; init; }
    public string EmploymentType { get; init; } = "Unknown";
    public string Remote { get; init; } = "Unknown";
    public string Seniority { get; init; } = "Unknown";
    public string? SalaryText { get; init; }
    public IReadOnlyList<string> RequiredSkills { get; init; } = [];
    public IReadOnlyList<string> NiceToHaveSkills { get; init; } = [];
    public IReadOnlyList<string> Responsibilities { get; init; } = [];
    public IReadOnlyList<string> Benefits { get; init; } = [];
    public string Summary { get; init; } = string.Empty;
    public IReadOnlyList<string> Keywords { get; init; } = [];
}