namespace JobHunt.Api.Features.Profile;

public sealed class ProfileRecord
{
    public int Id { get; set; }

    public string Json { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAt { get; set; }
}
