using System.Text.Json;
using System.Text.Json.Serialization;

namespace JobHunt.Api.Features.Jobs.Seek;

public sealed record SeekSearchResponse
{
    [JsonPropertyName("data")]
    public IReadOnlyList<SeekAd>? Data { get; init; }

    [JsonPropertyName("totalCount")]
    public int? TotalCount { get; init; }
}

public sealed record SeekAd
{
    [JsonPropertyName("id")]
    public JsonElement? Id { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("companyName")]
    public string? CompanyName { get; init; }

    [JsonPropertyName("advertiser")]
    public SeekAdvertiser? Advertiser { get; init; }

    [JsonPropertyName("location")]
    public string? Location { get; init; }

    [JsonPropertyName("workArrangement")]
    public string? WorkArrangement { get; init; }

    [JsonPropertyName("salary")]
    public string? Salary { get; init; }

    [JsonPropertyName("listingDate")]
    public string? ListingDate { get; init; }

    [JsonPropertyName("teaser")]
    public string? Teaser { get; init; }

    [JsonPropertyName("content")]
    public string? Content { get; init; }

    [JsonPropertyName("jobShortDescription")]
    public string? JobShortDescription { get; init; }
}

public sealed record SeekAdvertiser
{
    [JsonPropertyName("description")]
    public string? Description { get; init; }
}
