using JobHunt.Api.Features.Ranking;

namespace JobHunt.Api.Tests;

public sealed class MatchReRankerTests
{
    private readonly MatchReRanker _reRanker = new();

    [Fact]
    public void Leaves_the_score_untouched_below_the_minimum_application_count()
    {
        var insights = Insights(applications: 3, positive: 3, bands: [("120-149k", 3, 3)], models: []);

        var result = _reRanker.Apply(60, "120-149k", null, insights);

        Assert.Equal(60, result.Score);
        Assert.Null(result.Reason);
    }

    [Fact]
    public void Leaves_the_score_untouched_when_no_bucket_matches()
    {
        var insights = Insights(applications: 10, positive: 5, bands: [("120-149k", 7, 10)], models: []);

        var result = _reRanker.Apply(60, "200k+", null, insights);

        Assert.Equal(60, result.Score);
        Assert.Null(result.Reason);
    }

    [Fact]
    public void Boosts_when_a_matching_bucket_outperforms_the_average()
    {
        var insights = Insights(applications: 10, positive: 5, bands: [("120-149k", 7, 10)], models: []);

        var result = _reRanker.Apply(60, "120-149k", null, insights);

        Assert.Equal(65, result.Score);
        Assert.Contains("boosted +5", result.Reason);
        Assert.Contains("70%", result.Reason);
    }

    [Fact]
    public void Penalises_when_a_matching_bucket_underperforms_the_average()
    {
        var insights = Insights(applications: 10, positive: 5, bands: [("120-149k", 3, 10)], models: []);

        var result = _reRanker.Apply(60, "120-149k", null, insights);

        Assert.Equal(55, result.Score);
        Assert.Contains("penalised -5", result.Reason);
    }

    [Fact]
    public void Clamps_the_adjustment_to_the_maximum()
    {
        var insights = Insights(
            applications: 10,
            positive: 5,
            bands: [("120-149k", 10, 10)],
            models: [("Remote", 10, 10)]);

        var result = _reRanker.Apply(60, "120-149k", "Remote", insights);

        Assert.Equal(75, result.Score);
        Assert.Contains("boosted +15", result.Reason);
    }

    [Fact]
    public void Ignores_buckets_with_too_few_samples()
    {
        var insights = Insights(applications: 10, positive: 5, bands: [("120-149k", 1, 1)], models: []);

        var result = _reRanker.Apply(60, "120-149k", null, insights);

        Assert.Equal(60, result.Score);
        Assert.Null(result.Reason);
    }

    private static OutcomeInsights Insights(
        int applications,
        int positive,
        (string Key, int Positive, int Total)[] bands,
        (string Key, int Positive, int Total)[] models) => new()
    {
        ApplicationsWithOutcomes = applications,
        PositiveApplications = positive,
        Bands = bands.ToDictionary(band => band.Key, band => new OutcomeStat(band.Positive, band.Total)),
        WorkModels = models.ToDictionary(model => model.Key, model => new OutcomeStat(model.Positive, model.Total)),
    };
}
