using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using JobHunt.Api.Features.Resume;

namespace JobHunt.Api.Tests;

public sealed class DocxResumeTextExtractorTests
{
    [Fact]
    public async Task Extracts_text_from_a_docx_through_the_composite()
    {
        var extractor = CreateExtractor();

        var text = await extractor.ExtractAsync(
            BytesToStream(BuildDocx("Ada Lovelace", "Software Engineer")), "resume.docx");

        Assert.Contains("Ada Lovelace", text);
        Assert.Contains("Software Engineer", text);
    }

    [Fact]
    public async Task Reads_a_non_seekable_stream()
    {
        var extractor = new DocxResumeTextExtractor();
        using var nonSeekable = new NonSeekableStream(BytesToStream(BuildDocx("Ada Lovelace")));

        var text = await extractor.ExtractAsync(nonSeekable);

        Assert.Contains("Ada Lovelace", text);
    }

    [Fact]
    public async Task Rejects_a_corrupt_docx()
    {
        var extractor = new DocxResumeTextExtractor();

        await Assert.ThrowsAsync<InvalidDataException>(
            () => extractor.ExtractAsync(TextToStream("not really a docx")));
    }

    [Fact]
    public void Advertises_the_docx_extension() =>
        Assert.Contains(".docx", new DocxResumeTextExtractor().Extensions);

    private static ResumeTextExtractor CreateExtractor() =>
        new([new PlainTextResumeExtractor(), new PdfResumeTextExtractor(), new DocxResumeTextExtractor()]);

    private static byte[] BuildDocx(params string[] lines)
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            main.Document = new Document();

            var body = new Body();
            foreach (var line in lines)
            {
                body.AppendChild(new Paragraph(new Run(new Text(line))));
            }

            main.Document.Body = body;
        }

        return stream.ToArray();
    }

    private static Stream BytesToStream(byte[] bytes) => new MemoryStream(bytes);

    private static Stream TextToStream(string text) => new MemoryStream(Encoding.UTF8.GetBytes(text));

    private sealed class NonSeekableStream(Stream inner) : Stream
    {
        public override bool CanRead => inner.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() => inner.Flush();

        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
