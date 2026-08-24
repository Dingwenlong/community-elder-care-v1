using System.Diagnostics;
using CommunityElderCare.Core.Ai;
using CommunityElderCare.Core.CareEvents;
using CommunityElderCare.Core.CareWork;
using CommunityElderCare.Core.CheckIns;
using CommunityElderCare.Core.Common;
using CommunityElderCare.Core.Devices;
using CommunityElderCare.Core.Elders;
using CommunityElderCare.Core.Identity;
using CommunityElderCare.Infrastructure.Devices;
using CommunityElderCare.Infrastructure.Identity;
using CommunityElderCare.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CommunityElderCare.Infrastructure.Demo;

public sealed record DemoResetResult(
    int ElderCount,
    Guid MainElderId,
    DateTimeOffset BaseTime,
    long ElapsedMilliseconds);

public sealed class DemoMutationGate
{
    private readonly SemaphoreSlim _mutex = new(1, 1);

    public async Task<IDisposable> EnterAsync(CancellationToken cancellationToken)
    {
        await _mutex.WaitAsync(cancellationToken);
        return new Releaser(_mutex);
    }

    private sealed class Releaser(SemaphoreSlim mutex) : IDisposable
    {
        private bool _released;

        public void Dispose()
        {
            if (_released)
            {
                return;
            }
            _released = true;
            mutex.Release();
        }
    }
}

public sealed class DemoResetService(
    CommunityCareDbContext dbContext,
    IPasswordHasher<UserAccount> passwordHasher,
    IConfiguration configuration,
    TimeProvider timeProvider,
    DemoMutationGate mutationGate)
{
    public async Task<DemoResetResult> ResetAsync(
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        using var gate = await mutationGate.EnterAsync(cancellationToken);
        var stopwatch = Stopwatch.StartNew();
        var baseTime = timeProvider.GetUtcNow();
        var seed = DemoSeedBuilder.Build(20, 20260824, baseTime);
        var demoPassword = configuration["COMMUNITYCARE_DEMO_PASSWORD"];
        if (string.IsNullOrEmpty(demoPassword))
        {
            throw new InvalidOperationException("COMMUNITYCARE_DEMO_PASSWORD is required for reset.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await DeleteKnownDemoRowsAsync(cancellationToken);
        dbContext.ChangeTracker.Clear();

        dbContext.ElderProfiles.AddRange(seed.Elders);
        var accounts = DemoIdentitySeed.BuildAccounts(seed.MainElderId);
        foreach (var account in accounts)
        {
            account.SetPasswordHash(passwordHasher.HashPassword(account, demoPassword));
        }
        dbContext.UserAccounts.AddRange(accounts);
        dbContext.ConsentGrants.Add(ConsentGrant.Create(
            Guid.Parse("33333333-3333-3333-3333-333333333301"),
            seed.MainElderId,
            DemoIdentitySeed.FamilyUserId,
            [
                ConsentField.RecentStatus,
                ConsentField.CareEventSummary,
                ConsentField.VisitSummary,
                ConsentField.ReminderCompletion,
            ],
            baseTime,
            baseTime.AddYears(1),
            DemoIdentitySeed.ElderUserId));
        AddReminders(seed.MainElderId, baseTime);

        var rawDeviceToken = configuration["COMMUNITYCARE_DEVICE_TOKEN"];
        dbContext.Devices.Add(Device.Register(
            DemoDeviceIds.MainSosDevice,
            seed.MainElderId,
            "客厅 SOS 演示设备",
            string.IsNullOrWhiteSpace(rawDeviceToken)
                ? null
                : DeviceTokenValidator.HashToken(rawDeviceToken),
            baseTime));
        dbContext.AuditEntries.Add(new AuditEntry(
            Guid.NewGuid(),
            actor.UserId,
            actor.Role.ToString(),
            "DemoResetCompleted",
            "DemoDataset",
            seed.MainElderId,
            baseTime,
            "管理员确认恢复 20 人演示数据",
            null,
            "Ready"));
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        stopwatch.Stop();
        return new DemoResetResult(
            seed.Elders.Count,
            seed.MainElderId,
            seed.BaseTime,
            stopwatch.ElapsedMilliseconds);
    }

    private async Task DeleteKnownDemoRowsAsync(CancellationToken cancellationToken)
    {
        await dbContext.NotificationAttempts.Where(item => item.IsDemoData)
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.BackgroundJobRuns.Where(item => item.IsDemoData)
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.AuditEntries.Where(item => item.IsDemoData)
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.DeviceSignals.Where(item => item.IsDemoData)
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.AiDrafts.Where(item => item.IsDemoData)
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.MemoryCandidates.Where(item => item.IsDemoData)
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.FollowUps.Where(item => item.IsDemoData)
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.ServiceOrders.Where(item => item.IsDemoData)
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.VisitTasks.Where(item => item.IsDemoData)
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.ContactAttempts.Where(item =>
                dbContext.CareEvents.Any(parent => parent.Id == item.CareEventId && parent.IsDemoData))
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.CareEventTransitions.Where(item =>
                dbContext.CareEvents.Any(parent => parent.Id == item.CareEventId && parent.IsDemoData))
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.CareEventEvidence.Where(item =>
                dbContext.CareEvents.Any(parent => parent.Id == item.CareEventId && parent.IsDemoData))
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.CareEvents.Where(item => item.IsDemoData)
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.IdempotencyRecords.Where(item => item.IsDemoData)
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.CheckIns.Where(item => item.IsDemoData)
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.Reminders.Where(item => item.IsDemoData)
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.Devices.Where(item => item.IsDemoData)
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.AccessAuditRecords.Where(item => item.IsDemoData)
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.BreakGlassGrants.Where(item => item.IsDemoData)
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.ConsentGrantFields.Where(field =>
                dbContext.ConsentGrants.Any(parent => parent.Id == field.ConsentGrantId && parent.IsDemoData))
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.ConsentGrants.Where(item => item.IsDemoData)
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.UserAccounts.Where(item => item.IsDemoData)
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.EmergencyContacts.Where(item =>
                dbContext.ElderProfiles.Any(parent => parent.Id == item.ElderProfileId && parent.IsDemoData))
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.ServiceNeeds.Where(item =>
                dbContext.ElderProfiles.Any(parent => parent.Id == item.ElderProfileId && parent.IsDemoData))
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.HealthRisks.Where(item =>
                dbContext.ElderProfiles.Any(parent => parent.Id == item.ElderProfileId && parent.IsDemoData))
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.ElderProfiles.Where(item => item.IsDemoData)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private void AddReminders(Guid elderId, DateTimeOffset baseTime)
    {
        var dayStart = new DateTimeOffset(
            baseTime.Year,
            baseTime.Month,
            baseTime.Day,
            0,
            0,
            0,
            baseTime.Offset);
        dbContext.Reminders.AddRange(
            Reminder.Create(
                Guid.Parse("44444444-4444-4444-4444-444444444401"),
                elderId,
                ReminderType.Medication,
                "按既有医嘱查看今日服药提醒",
                dayStart.AddHours(8)),
            Reminder.Create(
                Guid.Parse("44444444-4444-4444-4444-444444444402"),
                elderId,
                ReminderType.FollowUpAppointment,
                "演示复诊预约提醒",
                dayStart.AddHours(10)),
            Reminder.Create(
                Guid.Parse("44444444-4444-4444-4444-444444444403"),
                elderId,
                ReminderType.CommunityActivity,
                "社区活动演示提醒",
                dayStart.AddHours(14)),
            Reminder.Create(
                Guid.Parse("44444444-4444-4444-4444-444444444404"),
                elderId,
                ReminderType.VisitSchedule,
                "上门探访演示提醒",
                dayStart.AddHours(16)));
    }
}
