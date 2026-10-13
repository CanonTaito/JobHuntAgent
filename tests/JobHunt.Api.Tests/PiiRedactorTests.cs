using JobHunt.Api.Features.Pii;
using JobHunt.Api.Features.Profile;
using JobHunt.Api.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace JobHunt.Api.Tests;

public sealed class PiiRedactorTests
{
    private static PiiRedactor Redactor(CandidateProfile profile) =>
        new(profile, new InMemoryPiiMappingStore(), NullLogger<PiiRedactor>.Instance);

    private static readonly CandidateProfile Profile = new()
    {
        Name = "Jordan Avery",
        Location = "Melbourne",
        Experience = [new ExperienceEntry { Company = "Harbour Tech" }],
        Education = [new EducationEntry { Institution = "RMIT" }],
    };

    [Theory]
    [InlineData("Hi Jordan,", "Hi [FIRST_NAME],")]
    [InlineData("Jordan Avery speaking", "[NAME] speaking")]
    [InlineData("Hello Avery", "Hello [LAST_NAME]")]
    [InlineData("I work at Harbour Tech.", "I work at [EMPLOYER_1].")]
    [InlineData("I studied at RMIT.", "I studied at [INSTITUTION_1].")]
    [InlineData("Based in Melbourne.", "Based in [LOCATION_1].")]
    public void Replaces_each_pii_value_with_its_placeholder(string input, string expected)
    {
        Assert.Equal(expected, Redactor(Profile).Redact(input));
    }

    [Fact]
    public void Does_not_replace_tokens_embedded_inside_a_larger_word()
    {
        var redactor = Redactor(new CandidateProfile { Name = "Ann Lee" });

        Assert.Equal("[FIRST_NAME] works on Annual reports", redactor.Redact("Ann works on Annual reports"));
    }

    [Fact]
    public void Rehydration_restores_the_original_text()
    {
        var redactor = Redactor(Profile);
        const string original = "Jordan Avery at Harbour Tech in Melbourne";

        var redacted = redactor.Redact(original);

        Assert.DoesNotContain("Jordan", redacted);
        Assert.DoesNotContain("Harbour", redacted);
        Assert.DoesNotContain("Melbourne", redacted);
        Assert.Equal(original, redactor.Rehydrate(redacted));
    }

    [Fact]
    public void Leaves_text_without_pii_untouched()
    {
        Assert.Equal("We build APIs.", Redactor(Profile).Redact("We build APIs."));
    }
}
