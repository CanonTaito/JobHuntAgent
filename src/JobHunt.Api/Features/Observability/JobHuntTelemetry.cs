using System.Diagnostics;

namespace JobHunt.Api.Features.Observability;

public static class JobHuntTelemetry
{
    public const string ServiceName = "JobHuntAgent";

    public const string ActivitySourceName = "JobHunt.Agent";

    public const string MeterName = "JobHunt.Agent";

    public static ActivitySource ActivitySource { get; } = new(ActivitySourceName);
}
