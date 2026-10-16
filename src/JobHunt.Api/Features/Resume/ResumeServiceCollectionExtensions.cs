using Microsoft.Extensions.DependencyInjection;

namespace JobHunt.Api.Features.Resume;

public static class ResumeServiceCollectionExtensions
{
    public static IServiceCollection AddResumeTextExtraction(this IServiceCollection services)
    {
        services.AddSingleton<PlainTextResumeExtractor>();
        services.AddSingleton<IResumeTextExtractor>(sp => sp.GetRequiredService<PlainTextResumeExtractor>());
        services.AddSingleton<ResumeTextExtractor>();

        return services;
    }
}
