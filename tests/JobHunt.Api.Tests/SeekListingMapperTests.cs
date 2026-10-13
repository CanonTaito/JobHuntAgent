using System.Text.Json;
using JobHunt.Api.Features.Jobs.Seek;

namespace JobHunt.Api.Tests;

public sealed class SeekListingMapperTests
{
    [Theory]
    [InlineData("$150,000 - $170,000", 150_000, 170_000)]
    [InlineData("$160k - $190k", 160_000, 190_000)]
    [InlineData("$120,000", 120_000, null)]
    [InlineData("$200k + super", 200_000, null)]
    [InlineData("Competitive", null, null)]
    [InlineData("", null, null)]
    [InlineData(null, null, null)]
    public void Parses_salary_ranges(string? text, int? expectedMin, int? expectedMax)
    {
        var (min, max) = SeekListingMapper.ParseSalary(text);

        Assert.Equal(expectedMin, min);
        Assert.Equal(expectedMax, max);
    }

    [Fact]
    public void Maps_a_full_ad_into_a_listing()
    {
        using var document = JsonDocument.Parse("12345");
        var ad = new SeekAd
        {
            Id = document.RootElement.Clone(),
            Title = "  Senior Engineer  ",
            CompanyName = "Acme",
            Location = " Melbourne, VIC ",
            WorkArrangement = "remote",
            Salary = "$150,000 - $170,000",
            ListingDate = "2026-10-01T00:00:00Z",
            Content = "Build things.",
        };

        var listing = SeekListingMapper.Map(ad);

        Assert.NotNull(listing);
        Assert.Equal("12345", listing!.Id);
        Assert.Equal("seek", listing.Source);
        Assert.Equal("Senior Engineer", listing.Title);
        Assert.Equal("Acme", listing.Company);
        Assert.Equal("Melbourne, VIC", listing.Location);
        Assert.Equal("Remote", listing.WorkModel);
        Assert.Equal(150_000, listing.SalaryMin);
        Assert.Equal(170_000, listing.SalaryMax);
        Assert.Equal("AUD", listing.SalaryCurrency);
        Assert.Equal("https://www.seek.com.au/job/12345", listing.Url);
        Assert.Equal(new DateOnly(2026, 10, 1), listing.PostedAt);
    }

    [Fact]
    public void Returns_null_when_the_title_is_missing()
    {
        using var document = JsonDocument.Parse("12345");
        var ad = new SeekAd { Id = document.RootElement.Clone(), Title = "   " };

        Assert.Null(SeekListingMapper.Map(ad));
    }

    [Theory]
    [InlineData("remote", "Remote")]
    [InlineData("Hybrid", "Hybrid")]
    [InlineData("onsite", "On-site")]
    [InlineData("on-site", "On-site")]
    [InlineData("flexible", null)]
    [InlineData(null, null)]
    public void Maps_work_arrangements(string? arrangement, string? expected)
    {
        Assert.Equal(expected, SeekListingMapper.MapWorkModel(arrangement));
    }
}
