using System.ClientModel;
using System.ClientModel.Primitives;
using System.Globalization;

namespace JobHunt.Api.Ai.Retry;

public static class AiErrorClassifier
{
    public static bool IsRetryable(Exception exception, out TimeSpan? retryAfter)
    {
        retryAfter = null;
        return Evaluate(exception, ref retryAfter);
    }

    private static bool Evaluate(Exception exception, ref TimeSpan? retryAfter)
    {
        switch (exception)
        {
            case AggregateException aggregate when aggregate.InnerExceptions.Count > 0:
                foreach (var inner in aggregate.InnerExceptions)
                {
                    if (!Evaluate(inner, ref retryAfter))
                    {
                        return false;
                    }
                }

                return true;
            case ClientResultException clientException when clientException.Status > 0:
                retryAfter ??= ReadRetryAfter(clientException.GetRawResponse());
                return IsRetryableStatus(clientException.Status);
            case ClientResultException { InnerException: { } transportFailure }:
                return Evaluate(transportFailure, ref retryAfter);
            case HttpRequestException:
            case TimeoutException:
            case OperationCanceledException:
                return true;
            default:
                return false;
        }
    }

    private static bool IsRetryableStatus(int status) =>
        status is 408 or 429 || status is >= 500 and <= 599;

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
