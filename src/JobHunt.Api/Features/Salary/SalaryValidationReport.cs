namespace JobHunt.Api.Features.Salary;

public sealed record SalaryValidationReport
{
    public int Sampled { get; init; }

    public int KnownSalary { get; init; }

    public int Validated { get; init; }

    public int Accurate { get; init; }

    public double? Accuracy { get; init; }

    public IReadOnlyList<SalaryValidationCheck> Checks { get; init; } = [];

    public IReadOnlyList<SalaryValidationInference> Inferred { get; init; } = [];
}

public sealed record SalaryValidationCheck
{
    public string SourceId { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string? DisplayedBand { get; init; }

    public string? InferredBand { get; init; }

    public bool Inferred { get; init; }

    public bool Matched { get; init; }
}

public sealed record SalaryValidationInference
{
    public string SourceId { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string? Band { get; init; }

    public string Source { get; init; } = string.Empty;
}
