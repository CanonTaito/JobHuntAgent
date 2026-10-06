namespace JobHunt.Api.Features.Pii;

public sealed class InMemoryPiiMappingStore : IPiiMappingStore
{
    private readonly List<PiiMapping> _mappings = [];
    private readonly HashSet<string> _values = new(StringComparer.OrdinalIgnoreCase);

    public void Put(PiiMapping mapping)
    {
        if (_values.Add(mapping.Value))
        {
            _mappings.Add(mapping);
        }
    }

    public IReadOnlyList<PiiMapping> GetAll() => _mappings;
}