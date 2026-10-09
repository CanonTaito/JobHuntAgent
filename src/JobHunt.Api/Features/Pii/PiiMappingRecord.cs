namespace JobHunt.Api.Features.Pii;

public sealed class PiiMappingRecord
{
    public int Id { get; set; }

    public string Value { get; set; } = string.Empty;

    public string Placeholder { get; set; } = string.Empty;
}
