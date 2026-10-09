namespace JobHunt.Api.Features.Jobs;

public sealed class InMemoryJobSource : IJobSource
{
    private static readonly IReadOnlyList<JobListing> Listings = BuildListings();

    public string Name => "in-memory";

    public Task<IReadOnlyList<JobListing>> SearchAsync(JobSearchQuery query, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        IEnumerable<JobListing> matches = Listings;

        if (query.Keywords.Count > 0)
        {
            matches = matches.Where(listing => query.Keywords.Any(keyword => Matches(listing, keyword)));
        }

        if (query.Locations.Count > 0)
        {
            matches = matches.Where(listing => query.Locations.Any(location => Contains(listing.Location, location)));
        }

        if (query.WorkModels.Count > 0)
        {
            matches = matches.Where(listing => query.WorkModels.Any(model => Equals(listing.WorkModel, model)));
        }

        IReadOnlyList<JobListing> results = matches
            .Take(query.MaxResults > 0 ? query.MaxResults : Listings.Count)
            .ToList();

        return Task.FromResult(results);
    }

    private static bool Matches(JobListing listing, string keyword) =>
        Contains(listing.Title, keyword) || Contains(listing.Description, keyword);

    private static bool Contains(string? value, string term) =>
        !string.IsNullOrEmpty(value) && value.Contains(term, StringComparison.OrdinalIgnoreCase);

    private static bool Equals(string? value, string term) =>
        string.Equals(value, term, StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyList<JobListing> BuildListings() =>
    [
        new()
        {
            Id = "inmem-1",
            Source = "in-memory",
            Title = "Senior .NET Engineer",
            Company = "Northwind Payments",
            Location = "Melbourne, VIC",
            WorkModel = "Hybrid",
            SalaryMin = 150000,
            SalaryMax = 185000,
            SalaryCurrency = "AUD",
            Url = "https://example.com/jobs/inmem-1",
            PostedAt = new DateOnly(2026, 9, 28),
            Description = """
                Northwind Payments is hiring a Senior .NET Engineer to join the Payments platform team.

                You will own services end to end — data model, API, tests, observability — and help
                migrate a monolith to .NET minimal APIs. We work with C#, ASP.NET Core, EF Core and
                SQL Server, plus Azure App Services, Service Bus and AKS. Event-driven services and
                OpenTelemetry tracing are part of the day job.

                We are pragmatic about testing, low ceremony, and async-first. You will mentor two
                mid-level engineers and drive code review.
                """,
        },
        new()
        {
            Id = "inmem-2",
            Source = "in-memory",
            Title = "Backend Engineer (.NET)",
            Company = "Lumen Health",
            Location = "Melbourne, VIC",
            WorkModel = "Remote",
            SalaryText = "Competitive",
            Url = "https://example.com/jobs/inmem-2",
            PostedAt = new DateOnly(2026, 10, 1),
            Description = """
                Lumen Health builds clinical workflow tools used across Australia.

                Join a small backend team building REST APIs in ASP.NET Core on top of SQL Server.
                You will design data models, write integration tests, and contribute to our event
                pipeline. Experience with EF Core, message queues (Kafka preferred) and cloud
                hosting is valued. Healthcare domain experience is a bonus, not a requirement.
                """,
        },
        new()
        {
            Id = "inmem-3",
            Source = "in-memory",
            Title = "Platform Engineer",
            Company = "Cobalt Cloud",
            Location = "Sydney, NSW",
            WorkModel = "Remote",
            SalaryMin = 140000,
            SalaryMax = 170000,
            SalaryCurrency = "AUD",
            Url = "https://example.com/jobs/inmem-3",
            PostedAt = new DateOnly(2026, 9, 22),
            Description = """
                Cobalt Cloud runs a Kubernetes-based platform for internal product teams.

                We are looking for a platform engineer comfortable with .NET and Go, container
                workloads on AKS, Terraform, and GitHub Actions CI/CD. You will improve developer
                experience, harden observability, and reduce mean time to diagnose incidents.
                """,
        },
        new()
        {
            Id = "inmem-4",
            Source = "in-memory",
            Title = "Full Stack Engineer (.NET / React)",
            Company = "Fernwood Digital",
            Location = "Melbourne, VIC",
            WorkModel = "On-site",
            SalaryMin = 130000,
            SalaryMax = 160000,
            SalaryCurrency = "AUD",
            Url = "https://example.com/jobs/inmem-4",
            PostedAt = new DateOnly(2026, 10, 3),
            Description = """
                Fernwood Digital builds customer portals for retail clients.

                You will work across ASP.NET Core APIs and a React + TypeScript front end. Expect a
                mix of feature work, API design, and pairing. We value clear communication, code
                review, and shipping iteratively.
                """,
        },
        new()
        {
            Id = "inmem-5",
            Source = "in-memory",
            Title = "Principal Software Engineer",
            Company = "Aurora Labs",
            Location = "Brisbane, QLD",
            WorkModel = "Hybrid",
            SalaryText = "Attractive package + equity",
            Url = "https://example.com/jobs/inmem-5",
            PostedAt = new DateOnly(2026, 9, 30),
            Description = """
                Aurora Labs is scaling a distributed data platform.

                As a principal engineer you will set technical direction across services written in
                C# and .NET, guide architecture reviews, and mentor senior engineers. Deep experience
                with distributed systems, SQL and NoSQL stores, and cloud infrastructure is required.
                """,
        },
    ];
}
