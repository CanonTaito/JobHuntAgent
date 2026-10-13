namespace JobHunt.Api.Features.Jobs.Seek;

public sealed class SeekRequestThrottle
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly TimeSpan _minInterval;
    private DateTimeOffset _lastRequest = DateTimeOffset.MinValue;

    public SeekRequestThrottle(JobsOptions options) =>
        _minInterval = TimeSpan.FromMilliseconds(Math.Max(0, options.Seek.MinRequestIntervalMs));

    public async Task WaitAsync(CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var elapsed = DateTimeOffset.UtcNow - _lastRequest;
            if (elapsed < _minInterval)
            {
                await Task.Delay(_minInterval - elapsed, ct);
            }

            _lastRequest = DateTimeOffset.UtcNow;
        }
        finally
        {
            _gate.Release();
        }
    }
}
