using Microsoft.Extensions.DependencyInjection;

namespace JobHunt.Api.Features.Jobs;

public static class JobsServiceCollectionExtensions
{
    public static IServiceCollection AddJobSources(this IServiceCollection services)
    {
        services.AddSingleton<IJobSource, InMemoryJobSource>();
        return services;
    }
}
