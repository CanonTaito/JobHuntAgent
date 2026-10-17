using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Core;

namespace JobHunt.Api.Features.Resume;

public sealed class PdfResumeTextExtractor : IResumeTextExtractor
{
    public IReadOnlyCollection<string> Extensions { get; } = [".pdf"];

    public Task<string> ExtractAsync(Stream content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (content.CanSeek)
        {
            return Task.FromResult(Extract(content, cancellationToken));
        }

        using var buffer = new MemoryStream();
        content.CopyTo(buffer);
        buffer.Position = 0;
        return Task.FromResult(Extract(buffer, cancellationToken));
    }

    private static string Extract(Stream seekable, CancellationToken cancellationToken)
    {
        try
        {
            using var document = PdfDocument.Open(seekable);

            var builder = new StringBuilder();
            foreach (var page in document.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var text = page.Text;
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(text.Trim());
            }

            return builder.ToString();
        }
        catch (PdfDocumentFormatException e)
        {
            throw new InvalidDataException("The resume PDF could not be read.", e);
        }
    }
}
