using Microsoft.Extensions.Logging;

namespace JobHunt.Api.Ai.Retry;

public sealed class AiRetryPolicy
{
    private readonly AiRetryOptions _options;
    private readonly ILogger _logger;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;

    public AiRetryPolicy(AiRetryOptions options, ILogger logger, Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        _options = options;
        _logger = logger;
        _delay = delay ?? ((timeSpan, token) => Task.Delay(timeSpan, token));
    }

    private int MaxAttempts => Math.Max(1, _options.MaxAttempts);

    public async Task<T> ExecuteAsync<T>(
        string operation,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        var attempt = 1;

        while (true)
        {
            try
            {
                return await action(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception) when (attempt < MaxAttempts && AiErrorClassifier.IsRetryable(exception, out var retryAfter))
            {
                var delay = ComputeDelay(attempt, retryAfter);

                _logger.LogWarning(
                    exception,
                    "AI call '{Operation}' attempt {Attempt}/{MaxAttempts} failed; retrying in {DelayMs} ms.",
                    operation,
                    attempt,
                    MaxAttempts,
                    (int)delay.TotalMilliseconds);

                await _delay(delay, cancellationToken).ConfigureAwait(false);
                attempt++;
            }
        }
    }

    private TimeSpan ComputeDelay(int attempt, TimeSpan? retryAfter)
    {
        if (retryAfter is { } hint && hint > TimeSpan.Zero)
        {
            return Cap(hint);
        }

        return Cap(TimeSpan.FromMilliseconds(_options.BaseDelayMs * Math.Pow(2, attempt - 1)));
    }

    private TimeSpan Cap(TimeSpan delay)
    {
        var max = TimeSpan.FromMilliseconds(Math.Max(0, _options.MaxDelayMs));
        return delay > max ? max : delay;
    }
}
