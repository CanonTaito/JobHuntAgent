using JobHunt.Api.Features.Jobs;
using JobHunt.Api.Features.Profile;
using JobHunt.Api.Features.Salary;
using JobHunt.Api.Tests.TestSupport;

namespace JobHunt.Api.Tests;

public sealed class SalaryInferenceValidatorTests
{
    [Fact]
    public async Task Reports_full_accuracy_when_displayed_and_probed_bands_agree()
    {
        var displayed = Listing("displayed", min: 140_000, max: 165_000);
        var hidden = Listing("hidden");
        var source = new FilteringJobSource([(displayed, 140_000, 165_000), (hidden, 200_000, 240_000)]);
        var validator = new SalaryInferenceValidator(source, new SalaryBandInference(source), new CandidateProfile());

        var report = await validator.ValidateAsync(10, CancellationToken.None);

        Assert.Equal(2, report.Sampled);
        Assert.Equal(1, report.KnownSalary);
        Assert.Equal(1, report.Validated);
        Assert.Equal(1, report.Accurate);
        Assert.Equal(1d, report.Accuracy);

        var check = Assert.Single(report.Checks);
        Assert.Equal("120-149k", check.DisplayedBand);
        Assert.Equal("120-149k", check.InferredBand);
        Assert.True(check.Matched);

        var inferred = Assert.Single(report.Inferred);
        Assert.Equal("200k+", inferred.Band);
        Assert.Equal("Inferred", inferred.Source);
    }

    [Fact]
    public async Task Reports_a_mismatch_when_the_displayed_band_disagrees_with_the_probe()
    {
        var misleading = Listing("misleading", min: 200_000, max: 200_000);
        var source = new FilteringJobSource([(misleading, 140_000, 165_000)]);
        var validator = new SalaryInferenceValidator(source, new SalaryBandInference(source), new CandidateProfile());

        var report = await validator.ValidateAsync(10, CancellationToken.None);

        Assert.Equal(1, report.Validated);
        Assert.Equal(0, report.Accurate);
        Assert.Equal(0d, report.Accuracy);

        var check = Assert.Single(report.Checks);
        Assert.Equal("200k+", check.DisplayedBand);
        Assert.Equal("120-149k", check.InferredBand);
        Assert.False(check.Matched);
    }

    [Fact]
    public async Task Accuracy_is_null_when_no_displayed_salaries_are_sampled()
    {
        var hidden = Listing("hidden");
        var source = new FilteringJobSource([(hidden, 200_000, 240_000)]);
        var validator = new SalaryInferenceValidator(source, new SalaryBandInference(source), new CandidateProfile());

        var report = await validator.ValidateAsync(10, CancellationToken.None);

        Assert.Equal(0, report.KnownSalary);
        Assert.Null(report.Accuracy);
        Assert.Single(report.Inferred);
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
