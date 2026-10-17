using Microsoft.Extensions.DependencyInjection;

namespace JobHunt.Api.Features.Resume;

public static class ResumeServiceCollectionExtensions
{
    public static IServiceCollection AddResumeParsing(this IServiceCollection services)
    {
        services.AddSingleton<PlainTextResumeExtractor>();
        services.AddSingleton<IResumeTextExtractor>(sp => sp.GetRequiredService<PlainTextResumeExtractor>());
        services.AddSingleton<PdfResumeTextExtractor>();
        services.AddSingleton<IResumeTextExtractor>(sp => sp.GetRequiredService<PdfResumeTextExtractor>());
        services.AddSingleton<DocxResumeTextExtractor>();
        services.AddSingleton<IResumeTextExtractor>(sp => sp.GetRequiredService<DocxResumeTextExtractor>());
        services.AddSingleton<ResumeTextExtractor>();
        services.AddSingleton<ResumeParserAgent>();

        return services;
    }
}
