using JobHunt.Api.Features.FollowUps;

namespace JobHunt.Api.Tests;

public sealed class FollowUpJsonTests
{
    [Fact]
    public void Parses_a_well_formed_draft()
    {
        const string json = """{ "subject": " Following up ", "body": " Hi there ", "sendAfterDays": 10 }""";

        var draft = FollowUpJson.Parse(json);

        Assert.Equal("Following up", draft.Subject);
        Assert.Equal("Hi there", draft.Body);
        Assert.Equal(10, draft.SendAfterDays);
    }

    [Fact]
    public void Defaults_send_after_days_when_absent()
    {
        var draft = FollowUpJson.Parse("""{ "subject": "S", "body": "B" }""");

        Assert.Equal(FollowUpJson.DefaultSendAfterDays, draft.SendAfterDays);
    }

    [Theory]
    [InlineData(1, FollowUpJson.MinSendAfterDays)]
    [InlineData(45, FollowUpJson.MaxSendAfterDays)]
    [InlineData(7, 7)]
    public void Clamps_send_after_days(int input, int expected)
    {
        var draft = FollowUpJson.Parse($$"""{ "subject": "S", "body": "B", "sendAfterDays": {{input}} }""");

        Assert.Equal(expected, draft.SendAfterDays);
    }

    [Fact]
    public void Extracts_the_object_from_surrounding_text()
    {
        var draft = FollowUpJson.Parse("Sure!\n```json\n{ \"subject\": \"S\", \"body\": \"B\" }\n```");

        Assert.Equal("S", draft.Subject);
    }

    [Theory]
    [InlineData("""{ "body": "B" }""")]
    [InlineData("""{ "subject": "S" }""")]
    [InlineData("not json")]
    public void Throws_when_the_draft_is_incomplete(string raw)
    {
        Assert.Throws<FollowUpFormatException>(() => FollowUpJson.Parse(raw));
    }
}
