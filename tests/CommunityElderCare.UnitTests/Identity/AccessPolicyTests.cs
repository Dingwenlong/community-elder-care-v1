using CommunityElderCare.Core.Identity;
using CommunityElderCare.Infrastructure.Identity;
using CommunityElderCare.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CommunityElderCare.UnitTests.Identity;

public sealed class AccessPolicyTests
{
    private static readonly DateTimeOffset FixedNow =
        new(2026, 8, 24, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Revoked_family_consent_denies_the_next_read()
    {
        await using var fixture = await PolicyFixture.CreateAsync(FixedNow);
        var grant = ConsentGrant.Create(
            Guid.NewGuid(),
            fixture.MainElderId,
            fixture.FamilyUserId,
            [ConsentField.RecentStatus],
            FixedNow,
            FixedNow.AddDays(1),
            fixture.ElderUserId);
        fixture.DbContext.ConsentGrants.Add(grant);
        await fixture.DbContext.SaveChangesAsync();

        Assert.True(await fixture.Policy.CanReadAsync(
            fixture.FamilyActor,
            fixture.MainElderId,
            ConsentField.RecentStatus,
            CancellationToken.None));

        grant.Revoke(FixedNow.AddMinutes(1), fixture.ElderUserId);
        await fixture.DbContext.SaveChangesAsync();

        Assert.False(await fixture.Policy.CanReadAsync(
            fixture.FamilyActor,
            fixture.MainElderId,
            ConsentField.RecentStatus,
            CancellationToken.None));
    }

    [Fact]
    public async Task Community_staff_cannot_cross_area_boundary()
    {
        await using var fixture = await PolicyFixture.CreateAsync(FixedNow);
        var actor = new ActorContext(Guid.NewGuid(), DemoRole.CommunityStaff, null, "A02", null);

        Assert.False(await fixture.Policy.CanReadAsync(
            actor,
            fixture.MainElderId,
            ConsentField.HealthRiskSummary,
            CancellationToken.None));
    }

    [Fact]
    public async Task Service_worker_is_limited_to_assigned_elder_and_task()
    {
        await using var fixture = await PolicyFixture.CreateAsync(FixedNow);
        var actor = new ActorContext(
            Guid.NewGuid(),
            DemoRole.ServiceWorker,
            fixture.MainElderId,
            null,
            Guid.NewGuid());

        Assert.True(await fixture.Policy.CanReadAsync(
            actor,
            fixture.MainElderId,
            ConsentField.VisitSummary,
            CancellationToken.None));
        Assert.False(await fixture.Policy.CanReadAsync(
            actor,
            fixture.OtherElderId,
            ConsentField.VisitSummary,
            CancellationToken.None));
    }

    [Fact]
    public async Task Elder_can_read_own_authorized_fields()
    {
        await using var fixture = await PolicyFixture.CreateAsync(FixedNow);

        Assert.True(await fixture.Policy.CanReadAsync(
            fixture.ElderActor,
            fixture.MainElderId,
            ConsentField.EmergencyContact,
            CancellationToken.None));
    }

    [Fact]
    public async Task Break_glass_access_expires_after_fifteen_minutes()
    {
        await using var fixture = await PolicyFixture.CreateAsync(FixedNow);
        var emergencyEventId = Guid.NewGuid();
        var actor = new ActorContext(
            fixture.CommunityUserId,
            DemoRole.CommunityStaff,
            null,
            "A02",
            emergencyEventId);
        fixture.DbContext.BreakGlassGrants.Add(BreakGlassGrant.Create(
            Guid.NewGuid(),
            fixture.MainElderId,
            fixture.CommunityUserId,
            emergencyEventId,
            "跨片区紧急协助演示",
            FixedNow,
            FixedNow.AddMinutes(15)));
        await fixture.DbContext.SaveChangesAsync();

        Assert.True(await fixture.Policy.CanReadAsync(
            actor,
            fixture.MainElderId,
            ConsentField.RecentStatus,
            CancellationToken.None));

        fixture.TimeProvider.SetUtcNow(FixedNow.AddMinutes(16));

        Assert.False(await fixture.Policy.CanReadAsync(
            actor,
            fixture.MainElderId,
            ConsentField.RecentStatus,
            CancellationToken.None));
    }

    private sealed class PolicyFixture : IAsyncDisposable
    {
        private PolicyFixture(
            SqliteConnection connection,
            CommunityCareDbContext dbContext,
            MutableTimeProvider timeProvider,
            Guid mainElderId,
            Guid otherElderId)
        {
            Connection = connection;
            DbContext = dbContext;
            TimeProvider = timeProvider;
            MainElderId = mainElderId;
            OtherElderId = otherElderId;
            Policy = new AccessPolicy(dbContext, timeProvider);
        }

        public SqliteConnection Connection { get; }
        public CommunityCareDbContext DbContext { get; }
        public MutableTimeProvider TimeProvider { get; }
        public AccessPolicy Policy { get; }
        public Guid MainElderId { get; }
        public Guid OtherElderId { get; }
        public Guid ElderUserId { get; } = Guid.NewGuid();
        public Guid FamilyUserId { get; } = Guid.NewGuid();
        public Guid CommunityUserId { get; } = Guid.NewGuid();

        public ActorContext ElderActor =>
            new(ElderUserId, DemoRole.Elder, MainElderId, null, null);

        public ActorContext FamilyActor =>
            new(FamilyUserId, DemoRole.Family, MainElderId, null, null);

        public static async Task<PolicyFixture> CreateAsync(DateTimeOffset now)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<CommunityCareDbContext>()
                .UseSqlite(connection)
                .Options;
            var dbContext = new CommunityCareDbContext(options);
            await dbContext.Database.EnsureCreatedAsync();
            var seed = DemoSeedBuilder.Build(20, 20260824, now);
            dbContext.ElderProfiles.AddRange(seed.Elders);
            await dbContext.SaveChangesAsync();

            return new PolicyFixture(
                connection,
                dbContext,
                new MutableTimeProvider(now),
                seed.MainElderId,
                seed.Elders[1].Id);
        }

        public async ValueTask DisposeAsync()
        {
            await DbContext.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }

    private sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void SetUtcNow(DateTimeOffset value) => _now = value;
    }
}
