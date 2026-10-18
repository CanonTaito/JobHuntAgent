namespace JobHunt.Api.Features.Applications;

public sealed record ApplicationView
{
    public int Id { get; init; }

    public string Source { get; init; } = string.Empty;

    public string SourceId { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Company { get; init; } = string.Empty;

    public string? Location { get; init; }

    public string? WorkModel { get; init; }

    public string? Url { get; init; }

    public int? SalaryMin { get; init; }

    public int? SalaryMax { get; init; }

    public string? SalaryCurrency { get; init; }

    public string? SalaryBand { get; init; }

    public string SalarySource { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public int? OverallScore { get; init; }

    public int? RankScore { get; init; }

    public string? RankReason { get; init; }

    public int? SimilarityScore { get; init; }

    public int? SkillsScore { get; init; }

    public int? SeniorityScore { get; init; }

    public int? FitScore { get; init; }

    public string? MatchSummary { get; init; }

    public IReadOnlyList<string> TailoredBullets { get; init; } = [];

    public string? CoverLetter { get; init; }

    public string? TailoredProfileJson { get; init; }

    public DateTimeOffset? TailoredAt { get; init; }

    public string? FollowUpSubject { get; init; }

    public string? FollowUpBody { get; init; }

    public int? FollowUpSendAfterDays { get; init; }

    public DateTimeOffset? FollowUpDraftedAt { get; init; }

    public DateOnly? PostedAt { get; init; }

    public DateTimeOffset DiscoveredAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }

    public static ApplicationView From(JobApplication application) => new()
    {
        Id = application.Id,
        Source = application.Source,
        SourceId = application.SourceId,
        Title = application.Title,
        Company = application.Company,
        Location = application.Location,
        WorkModel = application.WorkModel,
        Url = application.Url,
        SalaryMin = application.SalaryMin,
        SalaryMax = application.SalaryMax,
        SalaryCurrency = application.SalaryCurrency,
        SalaryBand = application.SalaryBand,
        SalarySource = application.SalarySource.ToString(),
        Status = application.Status.ToString(),
        OverallScore = application.OverallScore,
        RankScore = application.OverallScore,
        SimilarityScore = application.SimilarityScore,
        SkillsScore = application.SkillsScore,
        SeniorityScore = application.SeniorityScore,
        FitScore = application.FitScore,
        MatchSummary = application.MatchSummary,
        TailoredBullets = SplitBullets(application.TailoredBullets),
        CoverLetter = application.CoverLetter,
        TailoredProfileJson = application.TailoredProfileJson,
        TailoredAt = application.TailoredAt,
        FollowUpSubject = application.FollowUpSubject,
        FollowUpBody = application.FollowUpBody,
        FollowUpSendAfterDays = application.FollowUpSendAfterDays,
        FollowUpDraftedAt = application.FollowUpDraftedAt,
        PostedAt = application.PostedAt,
        DiscoveredAt = application.DiscoveredAt,
        UpdatedAt = application.UpdatedAt,
    };

    private static IReadOnlyList<string> SplitBullets(string? bullets) =>
        string.IsNullOrWhiteSpace(bullets)
            ? []
            : bullets.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}
