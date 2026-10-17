using System.Text.Json;
using System.Text.Json.Serialization;

namespace JobHunt.Api.Features.Jobs.Seek;

public sealed record SeekSearchResponse
{
    [JsonPropertyName("data")]
    public IReadOnlyList<SeekAd>? Data { get; init; }

    [JsonPropertyName("totalCount")]
    public int? TotalCount { get; init; }

    [JsonPropertyName("solMetadata")]
    public SeekSolMetadata? SolMetadata { get; init; }
}

public sealed record SeekSolMetadata
{
    [JsonPropertyName("pageSize")]
    public int? PageSize { get; init; }
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

    [JsonPropertyName("locations")]
    public IReadOnlyList<SeekLocation>? Locations { get; init; }

    [JsonPropertyName("workArrangements")]
    public SeekWorkArrangements? WorkArrangements { get; init; }

    [JsonPropertyName("salaryLabel")]
    public string? SalaryLabel { get; init; }

    [JsonPropertyName("listingDate")]
    public string? ListingDate { get; init; }

    [JsonPropertyName("teaser")]
    public string? Teaser { get; init; }

    [JsonPropertyName("bulletPoints")]
    public IReadOnlyList<string>? BulletPoints { get; init; }

    [JsonPropertyName("workTypes")]
    public IReadOnlyList<string>? WorkTypes { get; init; }

    [JsonPropertyName("roleId")]
    public string? RoleId { get; init; }
}

public sealed record SeekAdvertiser
{
    [JsonPropertyName("description")]
    public string? Description { get; init; }
}

public sealed record SeekLocation
{
    [JsonPropertyName("label")]
    public string? Label { get; init; }

    [JsonPropertyName("countryCode")]
    public string? CountryCode { get; init; }
}

public sealed record SeekWorkArrangements
{
    [JsonPropertyName("displayText")]
    public string? DisplayText { get; init; }

    [JsonPropertyName("data")]
    public IReadOnlyList<SeekWorkArrangement>? Data { get; init; }
}

public sealed record SeekWorkArrangement
{
    [JsonPropertyName("label")]
    public SeekLabel? Label { get; init; }
}

public sealed record SeekLabel
{
    [JsonPropertyName("text")]
    public string? Text { get; init; }
}
