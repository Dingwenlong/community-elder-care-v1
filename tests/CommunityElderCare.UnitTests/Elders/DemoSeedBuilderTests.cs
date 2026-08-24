using CommunityElderCare.Core.Elders;
using CommunityElderCare.Infrastructure.Persistence;

namespace CommunityElderCare.UnitTests.Elders;

public sealed class DemoSeedBuilderTests
{
    [Fact]
    public void Build_creates_twenty_repeatable_synthetic_profiles()
    {
        var baseTime = new DateTimeOffset(2026, 8, 24, 8, 0, 0, TimeSpan.Zero);

        var first = DemoSeedBuilder.Build(20, 20260824, baseTime);
        var second = DemoSeedBuilder.Build(20, 20260824, baseTime);

        Assert.Equal(20, first.Elders.Count);
        Assert.Equal(first.Elders.Select(x => x.Id), second.Elders.Select(x => x.Id));
        Assert.All(first.Elders, x => Assert.True(x.IsDemoData));
        Assert.Contains(first.Elders, x => x.AttentionLevel == CareAttentionLevel.High);
        Assert.True(first.Elders[0].NextCheckInDueAt < baseTime);
        Assert.Equal(["A01", "A02", "A03"], first.Elders.Select(x => x.AreaCode).Distinct().Order().ToArray());
        Assert.All(
            first.Elders.SelectMany(x => x.EmergencyContacts),
            contact => Assert.Matches("^1990000[0-9]{4}$", contact.PhoneNumber));
    }

    [Theory]
    [InlineData(14)]
    [InlineData(26)]
    public void Build_rejects_counts_outside_demo_range(int count)
    {
        var baseTime = new DateTimeOffset(2026, 8, 24, 8, 0, 0, TimeSpan.Zero);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DemoSeedBuilder.Build(count, 20260824, baseTime));
    }
}
