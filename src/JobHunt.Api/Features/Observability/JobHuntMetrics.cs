using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace JobHunt.Api.Features.Observability;

public sealed class JobHuntMetrics
{
    private readonly Counter<long> _applicationsDiscovered;
    private readonly Histogram<double> _matchScore;

    public JobHuntMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(JobHuntTelemetry.MeterName);
        _applicationsDiscovered = meter.CreateCounter<long>(
            "jobhunt.applications.discovered", "{application}", "Applications discovered and scored");
        _matchScore = meter.CreateHistogram<double>(
            "jobhunt.match.score", "{score}", "Overall match score (0-100)");
    }

    public void ApplicationDiscovered(string source) =>
        _applicationsDiscovered.Add(1, new TagList { { "source", source } });

    public void RecordMatchScore(int overall) => _matchScore.Record(overall);
}
