namespace JobHunt.Api.Features.Jobs;

public sealed class JobsOptions
{
    public const string SectionName = "Jobs";

    public string Source { get; set; } = "seek";

    public SeekOptions Seek { get; set; } = new();

    public sealed class SeekOptions
    {
        public string Endpoint { get; set; } = "https://www.seek.com.au/api/chalice-search/v4/search";

        public string SiteKey { get; set; } = "AU-Main";

        public string SourceSystem { get; set; } = "houston";

        public string UserAgent { get; set; } =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0 Safari/537.36";

        public int PageSize { get; set; } = 30;

        public int MaxPages { get; set; } = 2;

        public int MinRequestIntervalMs { get; set; } = 1000;

        public int CacheMinutes { get; set; } = 30;

        public int TimeoutSeconds { get; set; } = 15;
    }
}
