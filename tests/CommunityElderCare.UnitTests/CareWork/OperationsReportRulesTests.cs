using CommunityElderCare.Infrastructure.CareWork;

namespace CommunityElderCare.UnitTests.CareWork;

public sealed class OperationsReportRulesTests
{
    [Fact]
    public void Beijing_day_boundaries_are_inclusive_then_exclusive()
    {
        var range = new OperationsDateRange(new(2026, 8, 27), new(2026, 8, 27));
        Assert.True(range.Contains(DateTimeOffset.Parse("2026-08-26T16:00:00Z")));
        Assert.True(range.Contains(DateTimeOffset.Parse("2026-08-27T15:59:59Z")));
        Assert.False(range.Contains(DateTimeOffset.Parse("2026-08-27T16:00:00Z")));
        Assert.False(range.Contains(DateTimeOffset.Parse("2026-08-26T15:59:59Z")));
    }

    [Theory]
    [InlineData("=1+1")]
    [InlineData("  @SUM(1,2)")]
    [InlineData("\t=1+1")]
    [InlineData("\n+1")]
    [InlineData("＝1+1")]
    public void Csv_formula_cells_are_neutralized(string cell) =>
        Assert.StartsWith("\"'", OperationsReportService.EscapeCsv(cell));
}
