namespace JobHunt.Api.Features.Profile;

public sealed record CandidateProfile
{
    public string Name { get; init; } = string.Empty;

    public string? Headline { get; init; }

    public string? Location { get; init; }

    public int? YearsOfExperience { get; init; }

    public string? Seniority { get; init; }

    public string? Summary { get; init; }

    public IReadOnlyList<string> Skills { get; init; } = [];

    public IReadOnlyList<ExperienceEntry> Experience { get; init; } = [];

    public IReadOnlyList<EducationEntry> Education { get; init; } = [];

    public IReadOnlyList<string> Highlights { get; init; } = [];

    public ProfilePreferences Preferences { get; init; } = new();
}

public sealed record ExperienceEntry
{
    public string? Company { get; init; }

    public string? Title { get; init; }

    public string? Period { get; init; }

    public IReadOnlyList<string> Highlights { get; init; } = [];
}

public sealed record EducationEntry
{
    public string? Qualification { get; init; }

    public string? Institution { get; init; }

    public int? Year { get; init; }
}

public sealed record ProfilePreferences
{
    public IReadOnlyList<string> Roles { get; init; } = [];

    public IReadOnlyList<string> Locations { get; init; } = [];

    public IReadOnlyList<string> WorkModels { get; init; } = [];

    public IReadOnlyList<string> EmploymentTypes { get; init; } = [];

    public SalaryExpectation? SalaryExpectation { get; init; }
}

public sealed record SalaryExpectation
{
    public int? Min { get; init; }

    public int? Max { get; init; }

    public string? Currency { get; init; }
}