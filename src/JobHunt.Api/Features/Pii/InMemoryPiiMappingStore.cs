namespace JobHunt.Api.Features.Pii;

public sealed class InMemoryPiiMappingStore : IPiiMappingStore
{
    private readonly List<PiiMapping> _mappings = [];

    public void Put(PiiMapping mapping)
    {
        if (_mappings.All(existing => existing.Value != mapping.Value))
        {
            _mappings.Add(mapping);
        }
    }

    public IReadOnlyList<PiiMapping> GetAll() => _mappings;
}
