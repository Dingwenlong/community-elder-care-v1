using CommunityElderCare.Core.CheckIns;
using CommunityElderCare.Core.Identity;
using CommunityElderCare.Infrastructure.CheckIns;
using CommunityElderCare.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CommunityElderCare.UnitTests.CheckIns;

public sealed class CheckInServiceTests
{
    [Fact]
    public async Task Same_request_id_returns_the_original_check_in()
    {
        var now = new DateTimeOffset(2026, 8, 24, 8, 10, 0, TimeSpan.Zero);
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<CommunityCareDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var dbContext = new CommunityCareDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        var seed = DemoSeedBuilder.Build(20, 20260824, now);
        dbContext.ElderProfiles.AddRange(seed.Elders);
        await dbContext.SaveChangesAsync();
        var service = new CheckInService(dbContext, new FixedTimeProvider(now));
        var requestId = Guid.NewGuid();
        var actor = new ActorContext(Guid.NewGuid(), DemoRole.Elder, seed.MainElderId, null, null);

        var first = await service.RecordAsync(
            seed.MainElderId,
            requestId,
            now.AddMinutes(-1),
            actor,
            CancellationToken.None);
        var second = await service.RecordAsync(
            seed.MainElderId,
            requestId,
            now.AddMinutes(-1),
            actor,
            CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.Equal(first.Value!.Id, second.Value!.Id);
        Assert.False(first.Value.IsDuplicate);
        Assert.True(second.Value.IsDuplicate);
        Assert.Single(await dbContext.CheckIns.ToListAsync());
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
