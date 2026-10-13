using JobHunt.Api.Features.Applications;
using JobHunt.Api.Features.Approvals;

namespace JobHunt.Api.Tests;

public sealed class ApplicationStatusTransitionsTests
{
    [Theory]
    [InlineData(JobApplicationStatus.Discovered, JobApplicationStatus.Scored)]
    [InlineData(JobApplicationStatus.Scored, JobApplicationStatus.Tailored)]
    [InlineData(JobApplicationStatus.Shortlisted, JobApplicationStatus.Tailored)]
    [InlineData(JobApplicationStatus.Tailored, JobApplicationStatus.AwaitingApproval)]
    [InlineData(JobApplicationStatus.AwaitingApproval, JobApplicationStatus.Applied)]
    [InlineData(JobApplicationStatus.Applied, JobApplicationStatus.Archived)]
    public void Allows_documented_transitions(JobApplicationStatus from, JobApplicationStatus to)
    {
        Assert.True(ApplicationStatusTransitions.CanTransition(from, to));
    }

    [Theory]
    [InlineData(JobApplicationStatus.Discovered, JobApplicationStatus.Applied)]
    [InlineData(JobApplicationStatus.Scored, JobApplicationStatus.Applied)]
    [InlineData(JobApplicationStatus.Tailored, JobApplicationStatus.Applied)]
    [InlineData(JobApplicationStatus.Applied, JobApplicationStatus.Shortlisted)]
    [InlineData(JobApplicationStatus.Archived, JobApplicationStatus.Scored)]
    [InlineData(JobApplicationStatus.Rejected, JobApplicationStatus.Scored)]
    public void Rejects_undocumented_transitions(JobApplicationStatus from, JobApplicationStatus to)
    {
        Assert.False(ApplicationStatusTransitions.CanTransition(from, to));
    }

    [Fact]
    public void Only_the_approval_gate_can_reach_applied()
    {
        var sources = Enum.GetValues<JobApplicationStatus>()
            .Where(from => ApplicationStatusTransitions.CanTransition(from, JobApplicationStatus.Applied));

        Assert.Equal([JobApplicationStatus.AwaitingApproval], sources);
    }
}
