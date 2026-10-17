using JobHunt.Api.Features.Resume;

namespace JobHunt.Api.Tests;

public sealed class ResumeJsonParserTests
{
    private const string ValidProfile = """
        {
          "name": "[NAME]",
          "headline": "Senior Software Engineer",
          "yearsOfExperience": 8,
          "skills": ["C#", ".NET"],
          "experience": [{ "company": "Harbour Tech", "title": "Engineer", "period": "2023 - present", "highlights": ["Did things"] }],
          "highlights": ["Shipped a thing"]
        }
        """;

    [Fact]
    public void Parses_a_full_profile()
    {
        var profile = ResumeJsonParser.Parse(ValidProfile);

        Assert.Equal("[NAME]", profile.Name);
        Assert.Equal("Senior Software Engineer", profile.Headline);
        Assert.Equal(8, profile.YearsOfExperience);
        Assert.Contains("C#", profile.Skills);
        Assert.Single(profile.Experience);
        Assert.Equal("Harbour Tech", profile.Experience[0].Company);
    }

    [Fact]
    public void Ignores_surrounding_prose()
    {
        var raw = $"Here is the profile:\n{ValidProfile}\nThanks!";

        var profile = ResumeJsonParser.Parse(raw);

        Assert.Equal("Senior Software Engineer", profile.Headline);
    }

    [Fact]
    public void Rejects_output_without_json()
    {
        Assert.Throws<ResumeParseFormatException>(
            () => ResumeJsonParser.Parse("I could not read that resume."));
    }

    [Fact]
    public void Rejects_an_empty_object()
    {
        Assert.Throws<ResumeParseFormatException>(() => ResumeJsonParser.Parse("{}"));
    }
}
