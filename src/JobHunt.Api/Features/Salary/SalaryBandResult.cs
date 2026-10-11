namespace JobHunt.Api.Features.Salary;

public sealed record SalaryBandResult
{
    public SalaryBand? Band { get; init; }

    public SalarySource Source { get; init; }
}
