using JobHunt.Api.Features.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JobHunt.Api.Features.Pii;

public sealed class EfPiiMappingStore : IPiiMappingStore
{
    private readonly IDbContextFactory<JobHuntDbContext> _contexts;

    public EfPiiMappingStore(IDbContextFactory<JobHuntDbContext> contexts) => _contexts = contexts;

    public void Put(PiiMapping mapping)
    {
        using var db = _contexts.CreateDbContext();
        var exists = db.PiiMappings.Any(m => EF.Functions.Collate(m.Value, "NOCASE") == mapping.Value);
        if (exists)
        {
            return;
        }

        db.PiiMappings.Add(new PiiMappingRecord { Value = mapping.Value, Placeholder = mapping.Placeholder });
        db.SaveChanges();
    }

    public IReadOnlyList<PiiMapping> GetAll()
    {
        using var db = _contexts.CreateDbContext();
        return db.PiiMappings
            .AsNoTracking()
            .Select(m => new PiiMapping(m.Value, m.Placeholder))
            .ToList();
    }
}
