namespace JobHunt.Api.Features.Resume;

public sealed class PlainTextResumeExtractor : IResumeTextExtractor
{
    public IReadOnlyCollection<string> Extensions { get; } = [".txt", ".md", ".markdown"];

    public async Task<string> ExtractAsync(Stream content, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(content, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}
