using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JobHunt.Api.Features.Profile;

public static class ProfileServiceCollectionExtensions
{
    private const string SectionName = "Profile";
    private const string PersonalFileName = "profile.json";
    private const string SampleFileName = "profile.sample.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static IServiceCollection AddCandidateProfile(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddSingleton(serviceProvider =>
        {
            var logger = serviceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger(nameof(ProfileServiceCollectionExtensions));

            var path = ResolvePath(configuration, environment);
            var profile = Load(path);

            logger.LogInformation("Loaded candidate profile from {ProfilePath}", path);
            return profile;
        });

        return services;
    }

    private static string ResolvePath(IConfiguration configuration, IHostEnvironment environment)
    {
        var configured = configuration[$"{SectionName}:Path"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return Path.GetFullPath(configured, environment.ContentRootPath);
        }

        var personalPath = Path.Combine(environment.ContentRootPath, PersonalFileName);
        if (File.Exists(personalPath))
        {
            return personalPath;
        }

        return Path.Combine(environment.ContentRootPath, SampleFileName);
    }

    private static CandidateProfile Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Candidate profile not found at '{path}'. Copy {SampleFileName} next to the API project, " +
                $"or set {SectionName}:Path to point at your profile file.",
                path);
        }

        CandidateProfile? profile;
        try
        {
            profile = JsonSerializer.Deserialize<CandidateProfile>(File.ReadAllText(path), SerializerOptions);
        }
        catch (JsonException e)
        {
            throw new InvalidOperationException($"Candidate profile '{path}' is not valid JSON.", e);
        }

        if (profile is null || string.IsNullOrWhiteSpace(profile.Name))
        {
            throw new InvalidOperationException(
                $"Candidate profile '{path}' must contain a non-empty \"name\" (the only required field).");
        }

        return profile;
    }
}