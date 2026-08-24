using CommunityElderCare.Core.CareEvents;
using CommunityElderCare.Core.Devices;
using CommunityElderCare.Core.Elders;
using CommunityElderCare.Infrastructure.CareEvents;
using CommunityElderCare.Infrastructure.Devices;
using CommunityElderCare.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CommunityElderCare.UnitTests.Devices;

public sealed class DeviceSignalServiceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 24, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Duplicate_device_event_is_stored_and_correlated_once()
    {
        await using var fixture = await DeviceFixture.CreateAsync();
        var command = fixture.Command(DeviceSignalType.SosButton, Guid.NewGuid());

        var first = await fixture.Service.ReceiveAsync(command, fixture.HardwareIdentity, default);
        var second = await fixture.Service.ReceiveAsync(command, fixture.HardwareIdentity, default);

        Assert.True(first.IsSuccess);
        Assert.Equal(first.Value!.SignalId, second.Value!.SignalId);
        Assert.False(first.Value.IsDuplicate);
        Assert.True(second.Value.IsDuplicate);
        Assert.Single(await fixture.Db.DeviceSignals.ToListAsync());
        Assert.Single(await fixture.Db.CareEventEvidence.ToListAsync());
    }

    [Fact]
    public async Task Server_receive_time_controls_ordering_and_device_time_is_diagnostic_only()
    {
        await using var fixture = await DeviceFixture.CreateAsync();
        var reportedFuture = Now.AddYears(20);

        var result = await fixture.Service.ReceiveAsync(
            fixture.Command(DeviceSignalType.DeviceOffline, Guid.NewGuid(), reportedFuture),
            fixture.HardwareIdentity,
            default);

        var stored = await fixture.Db.DeviceSignals.SingleAsync();
        Assert.True(result.IsSuccess);
        Assert.Equal(reportedFuture, stored.DeviceTime);
        Assert.Equal(Now, stored.ReceivedAt);
        var careEvent = await fixture.Db.CareEvents.SingleAsync();
        Assert.Equal(Now, careEvent.OccurredAt);
    }

    [Fact]
    public async Task No_water_signal_maps_to_needs_confirmation()
    {
        await using var fixture = await DeviceFixture.CreateAsync();

        var result = await fixture.Service.ReceiveAsync(
            fixture.Command(DeviceSignalType.NoWaterActivity, Guid.NewGuid()),
            fixture.HardwareIdentity,
            default);

        var careEvent = await fixture.Db.CareEvents.SingleAsync();
        Assert.True(result.IsSuccess);
        Assert.Equal(CareEventLevel.NeedsConfirmation, careEvent.Level);
        Assert.Equal(CareEventSource.Device, careEvent.Source);
    }

    [Fact]
    public async Task Device_anomaly_within_thirty_minutes_merges_into_open_safety_event()
    {
        await using var fixture = await DeviceFixture.CreateAsync();
        await fixture.Service.ReceiveAsync(
            fixture.Command(DeviceSignalType.SosButton, Guid.NewGuid()),
            fixture.HardwareIdentity,
            default);
        fixture.Time.SetUtcNow(Now.AddMinutes(20));

        var merged = await fixture.Service.ReceiveAsync(
            fixture.Command(DeviceSignalType.NoWaterActivity, Guid.NewGuid()),
            fixture.HardwareIdentity,
            default);

        Assert.True(merged.IsSuccess);
        Assert.Single(await fixture.Db.CareEvents.ToListAsync());
        Assert.Equal(2, await fixture.Db.DeviceSignals.CountAsync());
        Assert.Equal(2, await fixture.Db.CareEventEvidence.CountAsync());
    }

    [Fact]
    public async Task Unknown_device_and_identity_mismatch_are_rejected()
    {
        await using var fixture = await DeviceFixture.CreateAsync();
        var command = fixture.Command(DeviceSignalType.SosButton, Guid.NewGuid());

        var unknown = await fixture.Service.ReceiveAsync(
            command with { DeviceId = Guid.NewGuid() },
            new DeviceSignalIdentity(Guid.NewGuid(), DeviceSignalOrigin.Hardware, null),
            default);
        var mismatch = await fixture.Service.ReceiveAsync(
            command,
            new DeviceSignalIdentity(Guid.NewGuid(), DeviceSignalOrigin.Hardware, null),
            default);

        Assert.Equal("UNKNOWN_DEVICE", unknown.ErrorCode);
        Assert.Equal("DEVICE_ID_MISMATCH", mismatch.ErrorCode);
        Assert.Empty(await fixture.Db.DeviceSignals.ToListAsync());
    }

    [Fact]
    public async Task Token_validator_rejects_invalid_token_and_unknown_device()
    {
        await using var fixture = await DeviceFixture.CreateAsync();
        var validator = new DeviceTokenValidator(fixture.Db);

        Assert.True(await validator.ValidateAsync(fixture.DeviceId, DeviceFixture.RawToken, default));
        Assert.False(await validator.ValidateAsync(fixture.DeviceId, "wrong-token", default));
        Assert.False(await validator.ValidateAsync(Guid.NewGuid(), DeviceFixture.RawToken, default));
    }

    private sealed class DeviceFixture : IAsyncDisposable
    {
        public const string RawToken = "unit-test-device-token";
        private readonly SqliteConnection _connection;

        private DeviceFixture(
            SqliteConnection connection,
            CommunityCareDbContext db,
            DeviceSignalService service,
            MutableTimeProvider time,
            Guid elderId,
            Guid deviceId)
        {
            _connection = connection;
            Db = db;
            Service = service;
            Time = time;
            ElderId = elderId;
            DeviceId = deviceId;
            HardwareIdentity = new DeviceSignalIdentity(
                deviceId,
                DeviceSignalOrigin.Hardware,
                null);
        }

        public CommunityCareDbContext Db { get; }
        public DeviceSignalService Service { get; }
        public MutableTimeProvider Time { get; }
        public Guid ElderId { get; }
        public Guid DeviceId { get; }
        public DeviceSignalIdentity HardwareIdentity { get; }

        public static async Task<DeviceFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var db = new CommunityCareDbContext(
                new DbContextOptionsBuilder<CommunityCareDbContext>()
                    .UseSqlite(connection)
                    .Options);
            await db.Database.EnsureCreatedAsync();
            var seed = DemoSeedBuilder.Build(20, 20260824, Now);
            db.ElderProfiles.AddRange(seed.Elders);
            var deviceId = Guid.Parse("77777777-7777-7777-7777-777777777701");
            db.Devices.Add(Device.Register(
                deviceId,
                seed.MainElderId,
                "单元测试 SOS 设备",
                DeviceTokenValidator.HashToken(RawToken),
                Now));
            await db.SaveChangesAsync();
            var time = new MutableTimeProvider(Now);
            var careEvents = new CareEventService(
                db,
                time,
                new EscalationPolicy(
                    TimeSpan.FromMinutes(2),
                    TimeSpan.FromMinutes(5),
                    TimeSpan.FromMinutes(10)));
            return new DeviceFixture(
                connection,
                db,
                new DeviceSignalService(db, careEvents, time),
                time,
                seed.MainElderId,
                deviceId);
        }

        public DeviceSignalCommand Command(
            DeviceSignalType type,
            Guid eventId,
            DateTimeOffset? deviceTime = null) => new(
            DeviceId,
            eventId,
            deviceTime ?? Now,
            type,
            type == DeviceSignalType.SosButton ? "Held2Seconds" : null);

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }

    private sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void SetUtcNow(DateTimeOffset now) => _now = now;
    }
}
