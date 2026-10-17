using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace JobHunt.Api.Features.Resume;

public sealed class DocxResumeTextExtractor : IResumeTextExtractor
{
    public IReadOnlyCollection<string> Extensions { get; } = [".docx"];

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
            using var document = WordprocessingDocument.Open(seekable, isEditable: false);

            var body = document.MainDocumentPart?.Document?.Body;
            if (body is null)
            {
                return string.Empty;
            }

            var lines = new List<string>();
            foreach (var paragraph in body.Descendants<Paragraph>())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var text = paragraph.InnerText;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    lines.Add(text.Trim());
                }
            }

            return string.Join('\n', lines);
        }
        catch (Exception e) when (e is OpenXmlPackageException or FileFormatException)
        {
            throw new InvalidDataException("The resume DOCX could not be read.", e);
        }
    }
}
