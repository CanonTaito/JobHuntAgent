using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace JobHunt.Api.Features.Jobs.Seek;

public sealed class SeekJobSource : IJobSource
{
    public const string SourceName = "seek";
    public const string HttpClientName = "seek";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _httpFactory;
    private readonly JobsOptions.SeekOptions _options;
    private readonly SeekRequestThrottle _throttle;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SeekJobSource> _logger;

    public SeekJobSource(
        IHttpClientFactory httpFactory,
        JobsOptions options,
        SeekRequestThrottle throttle,
        IMemoryCache cache,
        ILogger<SeekJobSource> logger)
    {
        _httpFactory = httpFactory;
        _options = options.Seek;
        _throttle = throttle;
        _cache = cache;
        _logger = logger;
    }

    public string Name => SourceName;

    public async Task<IReadOnlyList<JobListing>> SearchAsync(JobSearchQuery query, CancellationToken ct)
    {
        var cacheKey = BuildCacheKey(query);
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<JobListing>? cached) && cached is not null)
        {
            return cached;
        }

        var pageSize = Math.Max(1, _options.PageSize);
        var pages = Math.Clamp(_options.MaxPages, 1, 20);
        var limit = query.MaxResults > 0 ? query.MaxResults : int.MaxValue;
        var listings = new List<JobListing>();

        for (var page = 1; page <= pages; page++)
        {
            var payload = await FetchAsync(query, page, ct);
            var ads = payload?.Data;
            if (ads is null || ads.Count == 0)
            {
                break;
            }

            foreach (var ad in ads)
            {
                var listing = SeekListingMapper.Map(ad);
                if (listing is not null)
                {
                    listings.Add(listing);
                }

                if (listings.Count >= limit)
                {
                    break;
                }
            }

            if (listings.Count >= limit || ads.Count < pageSize)
            {
                break;
            }
        }

        if (listings.Count > limit)
        {
            listings = listings.GetRange(0, limit);
        }

        IReadOnlyList<JobListing> result = listings;
        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(Math.Max(1, _options.CacheMinutes)));
        return result;
    }

    private async Task<SeekSearchResponse?> FetchAsync(JobSearchQuery query, int page, CancellationToken ct)
    {
        var url = BuildUrl(query, page);
        await _throttle.WaitAsync(ct);

        try
        {
            using var client = _httpFactory.CreateClient(HttpClientName);
            using var response = await client.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("SEEK search returned {Status} for {Url}", (int)response.StatusCode, url);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            return await JsonSerializer.DeserializeAsync<SeekSearchResponse>(stream, JsonOptions, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "SEEK search request failed for {Url}", url);
            return null;
        }
    }

    private string BuildUrl(JobSearchQuery query, int page)
    {
        var parameters = new List<string>
        {
            $"siteKey={Uri.EscapeDataString(_options.SiteKey)}",
            $"sourcesystem={Uri.EscapeDataString(_options.SourceSystem)}",
            $"page={page}",
            $"pagesize={_options.PageSize}",
            "locale=en-AU",
        };

        if (query.Keywords.Count > 0)
        {
            parameters.Add($"keywords={Uri.EscapeDataString(string.Join(' ', query.Keywords))}");
        }

        if (query.Locations.Count > 0)
        {
            parameters.Add($"where={Uri.EscapeDataString(string.Join(' ', query.Locations))}");
        }

        var salaryRange = BuildSalaryRange(query);
        if (salaryRange is not null)
        {
            parameters.Add($"salaryrange={salaryRange}");
        }

        var arrangement = BuildArrangement(query);
        if (arrangement is not null)
        {
            parameters.Add($"workarrangement={arrangement}");
        }

        var separator = _options.Endpoint.Contains('?') ? '&' : '?';
        return $"{_options.Endpoint}{separator}{string.Join('&', parameters)}";
    }

    private static string? BuildSalaryRange(JobSearchQuery query) => (query.SalaryMin, query.SalaryMax) switch
    {
        (not null, not null) => $"{query.SalaryMin}-{query.SalaryMax}",
        (not null, null) => $"{query.SalaryMin}-",
        (null, not null) => $"-{query.SalaryMax}",
        _ => null,
    };

    private static string? BuildArrangement(JobSearchQuery query)
    {
        foreach (var model in query.WorkModels)
        {
            var mapped = SeekListingMapper.MapWorkModel(model);
            if (mapped is not null)
            {
                return mapped.ToLowerInvariant().Replace("-", string.Empty);
            }
        }

        return null;
    }

    private string BuildCacheKey(JobSearchQuery query)
    {
        static string Join(IReadOnlyList<string> values) =>
            string.Join(',', values.Select(value => value.Trim().ToLowerInvariant()).OrderBy(value => value));

        return string.Join(
            '|',
            SourceName,
            Join(query.Keywords),
            Join(query.Locations),
            Join(query.WorkModels),
            query.SalaryMin?.ToString() ?? "-",
            query.SalaryMax?.ToString() ?? "-",
            _options.PageSize,
            Math.Clamp(_options.MaxPages, 1, 20));
    }
}
