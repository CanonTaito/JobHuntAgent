using JobHunt.Api.Features.Match;

namespace JobHunt.Api.Tests;

public sealed class MatchScoreJsonTests
{
    [Fact]
    public void Parses_a_well_formed_object()
    {
        const string json = """
            {
              "skills": 85,
              "seniority": 80,
              "fit": 75,
              "summary": "Strong fit.",
              "strengths": ["C#", " EF Core "],
              "gaps": ["No Kubernetes"]
            }
            """;

        var score = MatchScoreJson.Parse(json);

        Assert.Equal(85, score.Skills);
        Assert.Equal(80, score.Seniority);
        Assert.Equal(75, score.Fit);
        Assert.Equal("Strong fit.", score.Summary);
        Assert.Equal(["C#", "EF Core"], score.Strengths);
        Assert.Equal(["No Kubernetes"], score.Gaps);
    }

    [Fact]
    public void Extracts_the_object_from_surrounding_prose_and_fences()
    {
        const string raw = """
            Here is the result:
            ```json
            { "skills": 70, "seniority": 60, "fit": 50 }
            ```
            Hope that helps!
            """;

        var score = MatchScoreJson.Parse(raw);

        Assert.Equal(70, score.Skills);
        Assert.Equal(60, score.Seniority);
        Assert.Equal(50, score.Fit);
        Assert.Empty(score.Strengths);
    }

    [Theory]
    [InlineData(150, 100)]
    [InlineData(-5, 0)]
    [InlineData(72.6, 73)]
    [InlineData(72.4, 72)]
    public void Clamps_and_rounds_each_signal(double input, int expected)
    {
        var json = $$"""{ "skills": {{input.ToString(System.Globalization.CultureInfo.InvariantCulture)}}, "seniority": 50, "fit": 50 }""";

        var score = MatchScoreJson.Parse(json);

        Assert.Equal(expected, score.Skills);
    }

    [Fact]
    public void Throws_when_a_required_signal_is_missing()
    {
        Assert.Throws<MatchScoreFormatException>(
            () => MatchScoreJson.Parse("""{ "skills": 70, "seniority": 60 }"""));
    }

    [Fact]
    public void Throws_when_no_json_object_is_present()
    {
        Assert.Throws<MatchScoreFormatException>(() => MatchScoreJson.Parse("no json here"));
    }

    [Fact]
    public void Throws_when_the_object_is_not_valid_json()
    {
        Assert.Throws<MatchScoreFormatException>(
            () => MatchScoreJson.Parse("{ skills: 70, seniority: 60, fit: 50 }"));
    }
}
