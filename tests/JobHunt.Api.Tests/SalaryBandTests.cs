using JobHunt.Api.Features.Salary;

namespace JobHunt.Api.Tests;

public sealed class SalaryBandTests
{
    [Theory]
    [InlineData(0, "Under 80k")]
    [InlineData(79_999, "Under 80k")]
    [InlineData(80_000, "80-99k")]
    [InlineData(99_999, "80-99k")]
    [InlineData(100_000, "100-119k")]
    [InlineData(119_999, "100-119k")]
    [InlineData(120_000, "120-149k")]
    [InlineData(149_999, "120-149k")]
    [InlineData(150_000, "150-199k")]
    [InlineData(199_999, "150-199k")]
    [InlineData(200_000, "200k+")]
    [InlineData(1_500_000, "200k+")]
    public void Classify_returns_the_band_containing_the_amount(int amount, string expected)
    {
        Assert.Equal(expected, SalaryBand.Classify(amount)!.Label);
    }

    [Fact]
    public void Classify_returns_null_for_amounts_below_zero()
    {
        Assert.Null(SalaryBand.Classify(-1));
    }

    [Fact]
    public void All_bands_are_ordered_and_contiguous()
    {
        Assert.Equal(
            ["Under 80k", "80-99k", "100-119k", "120-149k", "150-199k", "200k+"],
            SalaryBand.All.Select(band => band.Label));
    }
}
