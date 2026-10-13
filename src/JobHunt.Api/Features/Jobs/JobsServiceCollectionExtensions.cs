using JobHunt.Api.Features.Jobs.Seek;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JobHunt.Api.Features.Jobs;

public static class JobsServiceCollectionExtensions
{
    public static IServiceCollection AddJobSources(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(JobsOptions.SectionName).Get<JobsOptions>() ?? new JobsOptions();
        services.AddSingleton(options);

        services.AddMemoryCache();
        services.AddHttpClient(SeekJobSource.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.Seek.TimeoutSeconds));
            client.DefaultRequestHeaders.UserAgent.ParseAdd(options.Seek.UserAgent);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        });

        services.AddSingleton<InMemoryJobSource>();
        services.AddSingleton<SeekRequestThrottle>();
        services.AddSingleton<SeekJobSource>();
        services.AddSingleton<IJobSource>(provider =>
            options.Source.Equals(SeekJobSource.SourceName, StringComparison.OrdinalIgnoreCase)
                ? provider.GetRequiredService<SeekJobSource>()
                : provider.GetRequiredService<InMemoryJobSource>());

        return services;
    }
}
