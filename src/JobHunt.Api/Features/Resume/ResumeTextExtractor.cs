namespace JobHunt.Api.Features.Resume;

public sealed class ResumeTextExtractor
{
    private readonly Dictionary<string, IResumeTextExtractor> _byExtension;

    public ResumeTextExtractor(IEnumerable<IResumeTextExtractor> extractors)
    {
        ArgumentNullException.ThrowIfNull(extractors);

        _byExtension = new Dictionary<string, IResumeTextExtractor>(StringComparer.OrdinalIgnoreCase);
        foreach (var extractor in extractors)
        {
            foreach (var extension in extractor.Extensions)
            {
                _byExtension[extension] = extractor;
            }
        }
    }

    public IReadOnlyCollection<string> SupportedExtensions => _byExtension.Keys;

    public async Task<string> ExtractAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        var extension = Path.GetExtension(fileName);
        if (!_byExtension.TryGetValue(extension, out var extractor))
        {
            throw new NotSupportedException(
                $"Unsupported resume format '{extension}'. Supported formats: {string.Join(", ", SupportedExtensions)}.");
        }

        return await extractor.ExtractAsync(content, cancellationToken);
    }
}
