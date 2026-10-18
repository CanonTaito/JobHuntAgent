using System.ClientModel;
using System.ClientModel.Primitives;
using System.Globalization;

namespace JobHunt.Api.Ai.Retry;

public static class AiErrorClassifier
{
    private enum FailureKind
    {
        Transient = 0,
        Authentication = 1,
        ModelMissing = 2,
        Permanent = 3,
    }

    public static bool IsRetryable(Exception exception, out TimeSpan? retryAfter) =>
        Classify(exception, out retryAfter) == FailureKind.Transient;

    public static bool ShouldAdvance(Exception exception) =>
        Classify(exception, out _) != FailureKind.Permanent;

    private static FailureKind Classify(Exception exception, out TimeSpan? retryAfter)
    {
        TimeSpan? hint = null;
        var kind = Evaluate(exception, ref hint);
        retryAfter = hint;
        return kind;
    }

    private static FailureKind Evaluate(Exception exception, ref TimeSpan? retryAfter)
    {
        switch (exception)
        {
            case AggregateException aggregate when aggregate.InnerExceptions.Count > 0:
                var kind = FailureKind.Transient;
                foreach (var inner in aggregate.InnerExceptions)
                {
                    kind = (FailureKind)Math.Max((int)kind, (int)Evaluate(inner, ref retryAfter));
                }

                return kind;
            case ClientResultException clientException when clientException.Status > 0:
                retryAfter ??= ReadRetryAfter(clientException.GetRawResponse());
                return ClassifyStatus(clientException.Status);
            case ClientResultException { InnerException: { } transportFailure }:
                return Evaluate(transportFailure, ref retryAfter);
            case HttpRequestException:
            case TimeoutException:
            case OperationCanceledException:
                return FailureKind.Transient;
            default:
                return FailureKind.Permanent;
        }
    }

    private static FailureKind ClassifyStatus(int status) =>
        IsRetryableStatus(status)
            ? FailureKind.Transient
            : status is 401 or 403
                ? FailureKind.Authentication
                : status is 404
                    ? FailureKind.ModelMissing
                    : FailureKind.Permanent;

    private static bool IsRetryableStatus(int status) =>
        status is 408 or 429 || status is >= 500 and <= 599;

    public static int? ProviderFailureStatusCode(Exception exception) =>
        exception switch
        {
            ClientResultException { Status: > 0 } client => IsRetryableStatus(client.Status) ? 503 : 502,
            ClientResultException { InnerException: { } transportFailure } => ProviderFailureStatusCode(transportFailure),
            HttpRequestException => 503,
            TimeoutException => 503,
            _ => null,
        };

    private static TimeSpan? ReadRetryAfter(PipelineResponse? response)
    {
        if (response is null || !response.Headers.TryGetValue("Retry-After", out var value))
        {
            return null;
        }

        return ParseRetryAfter(value);
    }

    private static TimeSpan? ParseRetryAfter(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();

        if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds) && seconds >= 0)
        {
            return TimeSpan.FromSeconds(seconds);
        }

        if (DateTimeOffset.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var when))
        {
            var delay = when - DateTimeOffset.UtcNow;
            return delay > TimeSpan.Zero ? delay : TimeSpan.Zero;
        }

        return null;
    }
}
