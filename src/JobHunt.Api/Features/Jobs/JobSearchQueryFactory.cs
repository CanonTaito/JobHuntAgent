using JobHunt.Api.Features.Profile;

namespace JobHunt.Api.Features.Jobs;

public static class JobSearchQueryFactory
{
    public static JobSearchQuery FromProfile(CandidateProfile profile, int maxResults) => new()
    {
        Keywords = profile.Preferences.Roles,
        Locations = profile.Preferences.Locations,
        WorkModels = profile.Preferences.WorkModels,
        MaxResults = maxResults,
    };
}
