using JobHunt.Api.Features.Applications;

namespace JobHunt.Api.Features.Approvals;

public static class ApplicationStatusTransitions
{
    private static readonly IReadOnlyDictionary<JobApplicationStatus, JobApplicationStatus[]> Allowed =
        new Dictionary<JobApplicationStatus, JobApplicationStatus[]>
        {
            [JobApplicationStatus.Discovered] =
            [
                JobApplicationStatus.Scored,
                JobApplicationStatus.Shortlisted,
                JobApplicationStatus.Rejected,
                JobApplicationStatus.Archived,
            ],
            [JobApplicationStatus.Scored] =
            [
                JobApplicationStatus.Shortlisted,
                JobApplicationStatus.Tailored,
                JobApplicationStatus.Rejected,
                JobApplicationStatus.Archived,
            ],
            [JobApplicationStatus.Shortlisted] =
            [
                JobApplicationStatus.Tailored,
                JobApplicationStatus.Rejected,
                JobApplicationStatus.Archived,
            ],
            [JobApplicationStatus.Tailored] =
            [
                JobApplicationStatus.AwaitingApproval,
                JobApplicationStatus.Rejected,
                JobApplicationStatus.Archived,
            ],
            [JobApplicationStatus.AwaitingApproval] =
            [
                JobApplicationStatus.Applied,
                JobApplicationStatus.Rejected,
                JobApplicationStatus.Archived,
            ],
            [JobApplicationStatus.Applied] =
            [
                JobApplicationStatus.Archived,
            ],
            [JobApplicationStatus.Rejected] =
            [
                JobApplicationStatus.Archived,
            ],
            [JobApplicationStatus.Archived] = [],
        };

    public static bool CanTransition(JobApplicationStatus from, JobApplicationStatus to) =>
        Allowed.TryGetValue(from, out var targets) && targets.Contains(to);
}
