using JobHunt.Api.Features.Profile;
using Microsoft.Extensions.Logging;

namespace JobHunt.Api.Features.Pii;

public sealed class ProfileRedactorProvider
{
    private readonly IProfileStore _profiles;
    private readonly ILoggerFactory _loggerFactory;
    private readonly object _sync = new();

    private CandidateProfile? _builtFor;
    private PiiRedactor? _redactor;

    public ProfileRedactorProvider(IProfileStore profiles, ILoggerFactory loggerFactory)
    {
        _profiles = profiles;
        _loggerFactory = loggerFactory;
    }

    public CandidateProfile RedactCurrent()
    {
        var profile = _profiles.Current;
        return RedactorFor(profile).Redact(profile);
    }

    private PiiRedactor RedactorFor(CandidateProfile profile)
    {
        lock (_sync)
        {
            if (_redactor is null || !ReferenceEquals(_builtFor, profile))
            {
                _redactor = new PiiRedactor(
                    profile,
                    new InMemoryPiiMappingStore(),
                    _loggerFactory.CreateLogger<PiiRedactor>());
                _builtFor = profile;
            }

            return _redactor;
        }
    }
}
