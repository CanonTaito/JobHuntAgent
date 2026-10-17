using System.Text;
using JobHunt.Api.Features.Resume;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace JobHunt.Api.Tests;

public sealed class PdfResumeTextExtractorTests
{
    [Fact]
    public async Task Extracts_text_from_a_pdf_through_the_composite()
    {
        var extractor = CreateExtractor();

        var text = await extractor.ExtractAsync(
            BytesToStream(BuildPdf("Ada Lovelace", "Software Engineer")), "resume.pdf");

        Assert.Contains("Ada Lovelace", text);
        Assert.Contains("Software Engineer", text);
    }

    [Fact]
    public async Task Reads_a_non_seekable_stream()
    {
        var extractor = new PdfResumeTextExtractor();
        using var nonSeekable = new NonSeekableStream(BytesToStream(BuildPdf("Ada Lovelace")));

        var text = await extractor.ExtractAsync(nonSeekable);

        Assert.Contains("Ada Lovelace", text);
    }

    [Fact]
    public async Task Rejects_a_corrupt_pdf()
    {
        var extractor = new PdfResumeTextExtractor();

        await Assert.ThrowsAsync<InvalidDataException>(
            () => extractor.ExtractAsync(TextToStream("%PDF-1.7 not really a document")));
    }

    [Fact]
    public void Advertises_the_pdf_extension() =>
        Assert.Contains(".pdf", new PdfResumeTextExtractor().Extensions);

    private static ResumeTextExtractor CreateExtractor() =>
        new([new PlainTextResumeExtractor(), new PdfResumeTextExtractor()]);

    private static byte[] BuildPdf(params string[] lines)
    {
        var builder = new PdfDocumentBuilder();
        var page = builder.AddPage(595, 842);
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);

        var y = 760.0;
        foreach (var line in lines)
        {
            page.AddText(line, 12, new PdfPoint(50, y), font);
            y -= 20;
        }

        return builder.Build();
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
