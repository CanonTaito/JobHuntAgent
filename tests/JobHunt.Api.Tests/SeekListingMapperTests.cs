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
        using var document = JsonDocument.Parse("\"12345\"");
        var ad = new SeekAd
        {
            Id = document.RootElement.Clone(),
            Title = "  Senior Engineer  ",
            CompanyName = "Acme",
            Locations = [new SeekLocation { Label = " Melbourne, VIC " }],
            WorkArrangements = new SeekWorkArrangements { DisplayText = "remote" },
            SalaryLabel = "$150,000 - $170,000",
            ListingDate = "2026-10-01T00:00:00Z",
            Teaser = "  Build things.  ",
            BulletPoints = ["Ship it", ""],
        };

        var listing = SeekListingMapper.Map(ad);

        Assert.NotNull(listing);
        Assert.Equal("12345", listing!.Id);
        Assert.Equal("seek", listing.Source);
        Assert.Equal("Senior Engineer", listing.Title);
        Assert.Equal("Acme", listing.Company);
        Assert.Equal("Melbourne, VIC", listing.Location);
        Assert.Equal("Remote", listing.WorkModel);
        Assert.Equal("Build things.\n- Ship it", listing.Description);
        Assert.Equal(150_000, listing.SalaryMin);
        Assert.Equal(170_000, listing.SalaryMax);
        Assert.Equal("AUD", listing.SalaryCurrency);
        Assert.Equal("https://www.seek.com.au/job/12345", listing.Url);
        Assert.Equal(new DateOnly(2026, 10, 1), listing.PostedAt);
    }

    [Fact]
    public void Falls_back_to_the_work_arrangement_data_when_display_text_is_absent()
    {
        using var document = JsonDocument.Parse("\"1\"");
        var ad = new SeekAd
        {
            Id = document.RootElement.Clone(),
            Title = "Engineer",
            WorkArrangements = new SeekWorkArrangements
            {
                Data = [new SeekWorkArrangement { Label = new SeekLabel { Text = "On-site" } }],
            },
        };

        var listing = SeekListingMapper.Map(ad);

        Assert.NotNull(listing);
        Assert.Equal("On-site", listing!.WorkModel);
    }

    [Fact]
    public void Uses_the_advertiser_name_when_company_name_is_missing()
    {
        using var document = JsonDocument.Parse("\"42\"");
        var ad = new SeekAd
        {
            Id = document.RootElement.Clone(),
            Title = "Engineer",
            Advertiser = new SeekAdvertiser { Description = "  Halcyon Knights  " },
        };

        var listing = SeekListingMapper.Map(ad);

        Assert.NotNull(listing);
        Assert.Equal("Halcyon Knights", listing!.Company);
    }

    [Fact]
    public void Returns_null_when_the_title_is_missing()
    {
        using var document = JsonDocument.Parse("\"12345\"");
        var ad = new SeekAd
        {
            Id = document.RootElement.Clone(),
            Title = "   ",
        };

        var listing = SeekListingMapper.Map(ad);

        Assert.Null(listing);
    }

    [Theory]
    [InlineData("remote", "Remote")]
    [InlineData("hybrid", "Hybrid")]
    [InlineData("On-site", "On-site")]
    [InlineData("onsite", "On-site")]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void Maps_work_arrangements(string? arrangement, string? expected)
    {
        Assert.Equal(expected, SeekListingMapper.MapWorkModel(arrangement));
    }
}
