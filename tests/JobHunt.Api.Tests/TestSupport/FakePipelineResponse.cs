using System.ClientModel.Primitives;

namespace JobHunt.Api.Tests.TestSupport;

internal sealed class FakePipelineResponse : PipelineResponse
{
    private readonly int _status;
    private readonly FakePipelineResponseHeaders _headers;

    public FakePipelineResponse(int status, params (string Name, string Value)[] headers)
    {
        _status = status;
        _headers = new FakePipelineResponseHeaders(headers);
        ContentStream = new MemoryStream();
    }

    public override int Status => _status;

    public override string ReasonPhrase => string.Empty;

    public override Stream? ContentStream { get; set; }

    public override BinaryData Content => BinaryData.FromString(string.Empty);

    protected override PipelineResponseHeaders HeadersCore => _headers;

    public override void Dispose() => ContentStream?.Dispose();

    public override BinaryData BufferContent(CancellationToken cancellationToken) => Content;

    public override ValueTask<BinaryData> BufferContentAsync(CancellationToken cancellationToken) =>
        ValueTask.FromResult(Content);
}

internal sealed class FakePipelineResponseHeaders : PipelineResponseHeaders
{
    private readonly Dictionary<string, List<string>> _values = new(StringComparer.OrdinalIgnoreCase);

    public FakePipelineResponseHeaders(params (string Name, string Value)[] headers)
    {
        foreach (var (name, value) in headers)
        {
            if (!_values.TryGetValue(name, out var values))
            {
                values = [];
                _values[name] = values;
            }

            values.Add(value);
        }
    }

    public override bool TryGetValue(string name, out string? value)
    {
        if (_values.TryGetValue(name, out var values))
        {
            value = values[0];
            return true;
        }

        value = null;
        return false;
    }

    public override bool TryGetValues(string name, out IEnumerable<string>? values)
    {
        if (_values.TryGetValue(name, out var found))
        {
            values = found;
            return true;
        }

        values = null;
        return false;
    }

    public override IEnumerator<KeyValuePair<string, string>> GetEnumerator() =>
        _values
            .SelectMany(pair => pair.Value.Select(value => new KeyValuePair<string, string>(pair.Key, value)))
            .GetEnumerator();
}
