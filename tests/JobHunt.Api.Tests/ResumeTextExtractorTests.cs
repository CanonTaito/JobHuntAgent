using System.Text;
using JobHunt.Api.Features.Resume;
using Microsoft.Extensions.DependencyInjection;

namespace JobHunt.Api.Tests;

public sealed class ResumeTextExtractorTests
{
    [Fact]
    public async Task Extracts_plain_text_unchanged()
    {
        var extractor = CreateExtractor();
        const string resume = "Ada Lovelace\nSoftware Engineer\nC#";

        var text = await extractor.ExtractAsync(ToStream(resume), "resume.txt");

        Assert.Equal(resume, text);
    }

    [Fact]
    public async Task Extracts_markdown()
    {
        var extractor = CreateExtractor();
        const string resume = "# Ada Lovelace\n\n- C#\n- .NET";

        var text = await extractor.ExtractAsync(ToStream(resume), "resume.md");

        Assert.Equal(resume, text);
    }

    [Theory]
    [InlineData("RESUME.TXT")]
    [InlineData("Resume.Markdown")]
    public async Task Matches_the_extension_case_insensitively(string fileName)
    {
        var extractor = CreateExtractor();

        var text = await extractor.ExtractAsync(ToStream("Ada"), fileName);

        Assert.Equal("Ada", text);
    }

    [Fact]
    public async Task Rejects_an_unsupported_format()
    {
        var extractor = CreateExtractor();

        var exception = await Assert.ThrowsAsync<NotSupportedException>(
            () => extractor.ExtractAsync(ToStream("%PDF-1.7"), "resume.pdf"));

        Assert.Contains(".pdf", exception.Message);
        Assert.Contains(".txt", exception.Message);
    }

    [Fact]
    public async Task Rejects_a_file_without_an_extension()
    {
        var extractor = CreateExtractor();

        await Assert.ThrowsAsync<NotSupportedException>(
            () => extractor.ExtractAsync(ToStream("Ada"), "resume"));
    }

    [Fact]
    public void Registering_extraction_resolves_the_composite()
    {
        using var provider = new ServiceCollection()
            .AddResumeParsing()
            .BuildServiceProvider();

        var extractor = provider.GetRequiredService<ResumeTextExtractor>();

        Assert.Contains(".txt", extractor.SupportedExtensions);
    }

    private static ResumeTextExtractor CreateExtractor() =>
        new([new PlainTextResumeExtractor()]);

    private static Stream ToStream(string text) =>
        new MemoryStream(Encoding.UTF8.GetBytes(text));
}
