using JobHunt.Api.Features.Jobs;
using JobHunt.Api.Features.Salary;
using JobHunt.Api.Tests.TestSupport;

namespace JobHunt.Api.Tests;

public sealed class SalaryBandInferenceTests
{
    [Fact]
    public async Task Displayed_salary_is_classified_without_probing_the_source()
    {
        var source = new FilteringJobSource([]);
        var inference = new SalaryBandInference(source);

        var result = await inference.InferAsync(
            Listing("displayed", min: 140_000, max: 165_000),
            new JobSearchQuery(),
            CancellationToken.None);

        Assert.Equal(SalarySource.Displayed, result.Source);
        Assert.Equal("120-149k", result.Band!.Label);
        Assert.Equal(0, source.SearchCount);
    }

    [Theory]
    [InlineData(140_000, 165_000, "120-149k")]
    [InlineData(200_000, 240_000, "200k+")]
    [InlineData(85_000, 95_000, "80-99k")]
    public async Task Undisclosed_salary_is_inferred_from_the_lowest_surviving_band(
        int trueMin,
        int trueMax,
        string expectedBand)
    {
        var ad = Listing("hidden");
        var source = new FilteringJobSource([(ad, trueMin, trueMax)]);
        var inference = new SalaryBandInference(source);

        var result = await inference.InferAsync(ad, new JobSearchQuery(), CancellationToken.None);

        Assert.Equal(SalarySource.Inferred, result.Source);
        Assert.Equal(expectedBand, result.Band!.Label);
    }

    [Fact]
    public async Task Undisclosed_salary_is_unknown_when_no_band_matches()
    {
        var source = new FilteringJobSource([]);
        var inference = new SalaryBandInference(source);

        var result = await inference.InferAsync(Listing("ghost"), new JobSearchQuery(), CancellationToken.None);

        Assert.Equal(SalarySource.Unknown, result.Source);
        Assert.Null(result.Band);
    }

    [Fact]
    public async Task Probing_always_reports_the_inferred_source()
    {
        var ad = Listing("hidden");
        var source = new FilteringJobSource([(ad, 140_000, 165_000)]);
        var inference = new SalaryBandInference(source);

        var result = await inference.InferByProbingAsync(ad, new JobSearchQuery(), CancellationToken.None);

        Assert.Equal(SalarySource.Inferred, result.Source);
        Assert.Equal("120-149k", result.Band!.Label);
    }

    private static JobListing Listing(string id, int? min = null, int? max = null) => new()
    {
        Id = id,
        Source = "fake",
        Title = "Engineer",
        Company = "Acme",
        Description = "Engineer role",
        SalaryMin = min,
        SalaryMax = max,
    };
}
