using System.Text.Json;
using JobHunt.Api.Features.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobHunt.Api.Features.Profile;

public sealed class EfProfileStore : IProfileStore
{
    private const int ProfileId = 1;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IDbContextFactory<JobHuntDbContext> _contexts;
    private readonly CandidateProfile _fallback;
    private readonly ILogger<EfProfileStore> _logger;
    private readonly object _sync = new();

    private CandidateProfile? _current;

    public EfProfileStore(
        IDbContextFactory<JobHuntDbContext> contexts,
        CandidateProfile fallback,
        ILogger<EfProfileStore> logger)
    {
        _contexts = contexts;
        _fallback = fallback;
        _logger = logger;
    }

    public CandidateProfile Current
    {
        get
        {
            if (_current is not null)
            {
                return _current;
            }

            lock (_sync)
            {
                _current ??= Load() ?? _fallback;
                return _current;
            }
        }
    }

    public Task<CandidateProfile> GetAsync(CancellationToken ct = default) => Task.FromResult(Current);

    public async Task SaveAsync(CandidateProfile profile, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(profile, SerializerOptions);

        await using var db = await _contexts.CreateDbContextAsync(ct);
        var record = await db.Profiles.FirstOrDefaultAsync(p => p.Id == ProfileId, ct);
        if (record is null)
        {
            db.Profiles.Add(new ProfileRecord { Id = ProfileId, Json = json, UpdatedAt = DateTimeOffset.UtcNow });
        }
        else
        {
            record.Json = json;
            record.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct);

        lock (_sync)
        {
            _current = profile;
        }

        _logger.LogInformation("Saved candidate profile for {Name}", profile.Name);
    }

    private CandidateProfile? Load()
    {
        using var db = _contexts.CreateDbContext();
        var record = db.Profiles.AsNoTracking().FirstOrDefault(p => p.Id == ProfileId);
        if (string.IsNullOrWhiteSpace(record?.Json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<CandidateProfile>(record.Json, SerializerOptions);
        }
        catch (JsonException e)
        {
            _logger.LogWarning(e, "Stored candidate profile could not be read; using the startup profile instead");
            return null;
        }
    }
}
