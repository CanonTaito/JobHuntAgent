namespace JobHunt.Api.Features.Resume;

public interface IResumeTextExtractor
{
    IReadOnlyCollection<string> Extensions { get; }

    Task<string> ExtractAsync(Stream content, CancellationToken cancellationToken = default);
}
