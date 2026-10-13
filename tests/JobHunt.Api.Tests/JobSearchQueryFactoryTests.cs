using JobHunt.Api.Features.Jobs;
using JobHunt.Api.Features.Profile;

namespace JobHunt.Api.Tests;

public sealed class JobSearchQueryFactoryTests
{
    [Fact]
    public void Copies_preferences_and_caps_results()
    {
        var profile = new CandidateProfile
        {
            Preferences = new ProfilePreferences
            {
                Roles = ["Engineer"],
                Locations = ["Melbourne"],
                WorkModels = ["Remote"],
            },
        };

        var query = JobSearchQueryFactory.FromProfile(profile, 7);

        Assert.Equal(["Engineer"], query.Keywords);
        Assert.Equal(["Melbourne"], query.Locations);
        Assert.Equal(["Remote"], query.WorkModels);
        Assert.Equal(7, query.MaxResults);
        Assert.Null(query.SalaryMin);
        Assert.Null(query.SalaryMax);
    }
}
