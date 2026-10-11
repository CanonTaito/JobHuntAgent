namespace JobHunt.Api.Features.Salary;

public sealed record SalaryBand
{
    public string Label { get; init; } = string.Empty;

    public int Min { get; init; }

    public int? Max { get; init; }

    public static IReadOnlyList<SalaryBand> All { get; } =
    [
        new() { Label = "Under 80k", Min = 0, Max = 79_999 },
        new() { Label = "80-99k", Min = 80_000, Max = 99_999 },
        new() { Label = "100-119k", Min = 100_000, Max = 119_999 },
        new() { Label = "120-149k", Min = 120_000, Max = 149_999 },
        new() { Label = "150-199k", Min = 150_000, Max = 199_999 },
        new() { Label = "200k+", Min = 200_000, Max = null },
    ];

    public static SalaryBand? Classify(int amount)
    {
        foreach (var band in All)
        {
            if (amount >= band.Min && (band.Max is null || amount <= band.Max))
            {
                return band;
            }
        }

        return null;
    }
}
