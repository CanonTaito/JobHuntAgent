using JobHunt.Api.Features.Salary;

namespace JobHunt.Api.Features.Applications;

public sealed class JobApplication
{
    public int Id { get; set; }

    public string SourceId { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Company { get; set; } = string.Empty;

    public string? Location { get; set; }

    public string? WorkModel { get; set; }

    public string? Url { get; set; }

    public string Description { get; set; } = string.Empty;

    public int? SalaryMin { get; set; }

    public int? SalaryMax { get; set; }

    public string? SalaryCurrency { get; set; }

    public string? SalaryBand { get; set; }

    public SalarySource SalarySource { get; set; }

    public JobApplicationStatus Status { get; set; }

    public int? OverallScore { get; set; }

    public int? SimilarityScore { get; set; }

    public int? SkillsScore { get; set; }

    public int? SeniorityScore { get; set; }

    public int? FitScore { get; set; }

    public string? MatchSummary { get; set; }

    public string? TailoredBullets { get; set; }

    public string? CoverLetter { get; set; }

    public DateTimeOffset? TailoredAt { get; set; }

    public string? FollowUpSubject { get; set; }

    public string? FollowUpBody { get; set; }

    public int? FollowUpSendAfterDays { get; set; }

    public DateTimeOffset? FollowUpDraftedAt { get; set; }

    public DateOnly? PostedAt { get; set; }

    public DateTimeOffset DiscoveredAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

public enum JobApplicationStatus
{
    Discovered,
    Scored,
    Shortlisted,
    Tailored,
    AwaitingApproval,
    Applied,
    Rejected,
    Archived,
}
