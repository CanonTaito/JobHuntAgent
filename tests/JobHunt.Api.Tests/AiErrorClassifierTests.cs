using System.ClientModel;
using JobHunt.Api.Ai.Retry;
using JobHunt.Api.Tests.TestSupport;

namespace JobHunt.Api.Tests;

public sealed class AiErrorClassifierTests
{
    private static ClientResultException HttpError(int status, params (string Name, string Value)[] headers) =>
        new(new FakePipelineResponse(status, headers));

    [Theory]
    [InlineData(408)]
    [InlineData(429)]
    [InlineData(500)]
    [InlineData(502)]
    [InlineData(503)]
    [InlineData(504)]
    public void Treats_transient_http_statuses_as_retryable(int status) =>
        Assert.True(AiErrorClassifier.IsRetryable(HttpError(status), out _));

    [Theory]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(404)]
    [InlineData(422)]
    public void Treats_client_errors_as_permanent(int status) =>
        Assert.False(AiErrorClassifier.IsRetryable(HttpError(status), out _));

    [Fact]
    public void Reads_retry_after_seconds_from_a_rate_limited_response()
    {
        Assert.True(AiErrorClassifier.IsRetryable(HttpError(429, ("Retry-After", "2")), out var retryAfter));
        Assert.Equal(TimeSpan.FromSeconds(2), retryAfter);
    }

    [Fact]
    public void Retries_server_errors_that_have_no_retry_after_header()
    {
        Assert.True(AiErrorClassifier.IsRetryable(HttpError(503), out var retryAfter));
        Assert.Null(retryAfter);
    }

    [Fact]
    public void Ignores_an_unparseable_retry_after_header()
    {
        Assert.True(AiErrorClassifier.IsRetryable(HttpError(429, ("Retry-After", "not-a-date")), out var retryAfter));
        Assert.Null(retryAfter);
    }

    [Fact]
    public void Retries_network_and_timeout_failures()
    {
        Assert.True(AiErrorClassifier.IsRetryable(new HttpRequestException("network"), out _));
        Assert.True(AiErrorClassifier.IsRetryable(new TimeoutException(), out _));
    }

    [Fact]
    public void Retries_a_transport_failure_that_the_sdk_reports_as_a_h_less_client_error()
    {
        var exception = new ClientResultException(new FakePipelineResponse(0), new HttpRequestException("refused"));

        Assert.True(AiErrorClassifier.IsRetryable(exception, out _));
    }

    [Fact]
    public void Retries_the_aggregate_of_transport_failures_the_sdk_throws_after_its_own_retries()
    {
        var exception = new AggregateException(
            new ClientResultException(new FakePipelineResponse(0), new HttpRequestException("refused 1")),
            new ClientResultException(new FakePipelineResponse(0), new HttpRequestException("refused 2")));

        Assert.True(AiErrorClassifier.IsRetryable(exception, out _));
    }

    [Fact]
    public void Does_not_retry_an_aggregate_that_contains_a_permanent_failure()
    {
        var exception = new AggregateException(
            new ClientResultException(new FakePipelineResponse(503)),
            new ClientResultException(new FakePipelineResponse(400)));

        Assert.False(AiErrorClassifier.IsRetryable(exception, out _));
    }

    [Fact]
    public void Does_not_retry_unrecognised_failures() =>
        Assert.False(AiErrorClassifier.IsRetryable(new InvalidOperationException("boom"), out _));

    [Theory]
    [InlineData(429)]
    [InlineData(500)]
    public void Advances_past_transient_failures(int status) =>
        Assert.True(AiErrorClassifier.ShouldAdvance(HttpError(status)));

    [Theory]
    [InlineData(401)]
    [InlineData(403)]
    public void Advances_past_authentication_failures(int status) =>
        Assert.True(AiErrorClassifier.ShouldAdvance(HttpError(status)));

    [Theory]
    [InlineData(400)]
    [InlineData(404)]
    [InlineData(422)]
    public void Does_not_advance_past_permanent_failures(int status) =>
        Assert.False(AiErrorClassifier.ShouldAdvance(HttpError(status)));

    [Fact]
    public void Advances_past_network_failures() =>
        Assert.True(AiErrorClassifier.ShouldAdvance(new HttpRequestException("network")));

    [Fact]
    public void Does_not_advance_past_unrecognised_failures() =>
        Assert.False(AiErrorClassifier.ShouldAdvance(new InvalidOperationException("boom")));

    [Fact]
    public void Advances_past_an_aggregate_of_transient_failures()
    {
        var exception = new AggregateException(
            new ClientResultException(new FakePipelineResponse(503)),
            new ClientResultException(new FakePipelineResponse(429)));

        Assert.True(AiErrorClassifier.ShouldAdvance(exception));
    }

    [Fact]
    public void Advances_past_an_aggregate_that_contains_an_authentication_failure()
    {
        var exception = new AggregateException(
            new ClientResultException(new FakePipelineResponse(429)),
            new ClientResultException(new FakePipelineResponse(401)));

        Assert.True(AiErrorClassifier.ShouldAdvance(exception));
    }

    [Fact]
    public void Does_not_advance_past_an_aggregate_that_contains_a_permanent_failure()
    {
        var exception = new AggregateException(
            new ClientResultException(new FakePipelineResponse(401)),
            new ClientResultException(new FakePipelineResponse(400)));

        Assert.False(AiErrorClassifier.ShouldAdvance(exception));
    }
}
