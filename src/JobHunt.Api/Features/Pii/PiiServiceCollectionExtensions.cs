using JobHunt.Api.Features.Profile;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JobHunt.Api.Features.Pii;

public static class PiiServiceCollectionExtensions
{
    public static IServiceCollection AddPiiProtection(this IServiceCollection services)
    {
        services.AddSingleton<IPiiMappingStore, InMemoryPiiMappingStore>();
        services.AddSingleton(serviceProvider => new PiiRedactor(
            serviceProvider.GetRequiredService<CandidateProfile>(),
            serviceProvider.GetRequiredService<IPiiMappingStore>(),
            serviceProvider.GetRequiredService<ILogger<PiiRedactor>>()));

        return services;
    }
}