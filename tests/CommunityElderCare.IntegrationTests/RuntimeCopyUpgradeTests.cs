using CommunityElderCare.Core.CareEvents;
using CommunityElderCare.Core.CheckIns;
using CommunityElderCare.Core.Common;
using CommunityElderCare.Core.Devices;
using CommunityElderCare.Core.Identity;
using CommunityElderCare.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CommunityElderCare.IntegrationTests;

public sealed class RuntimeCopyUpgradeTests
{
    [Fact]
    public async Task Upgrade_rewrites_only_known_legacy_system_copy_and_is_idempotent()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<CommunityCareDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new CommunityCareDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var now = new DateTimeOffset(2026, 8, 25, 8, 0, 0, TimeSpan.Zero);
        var seed = DemoSeedBuilder.Build(20, 20260824, now);
        db.ElderProfiles.AddRange(seed.Elders);
        db.Reminders.AddRange(
            Reminder.Create(
                Guid.Parse("44444444-4444-4444-4444-444444444402"),
                seed.MainElderId,
                ReminderType.FollowUpAppointment,
                "演示复诊预约提醒",
                now),
            Reminder.Create(
                Guid.Parse("44444444-4444-4444-4444-444444444403"),
                seed.MainElderId,
                ReminderType.CommunityActivity,
                "社区活动演示提醒",
                now),
            Reminder.Create(
                Guid.Parse("44444444-4444-4444-4444-444444444404"),
                seed.MainElderId,
                ReminderType.VisitSchedule,
                "上门探访演示提醒",
                now));
        db.Devices.Add(Device.Register(
            DemoDeviceIds.MainSosDevice,
            seed.MainElderId,
            "客厅 SOS 演示设备",
            null,
            now));

        var careEvent = CareEvent.Create(
            Guid.NewGuid(),
            seed.MainElderId,
            CareEventCategory.SafetyHealth,
            CareEventLevel.NeedsConfirmation,
            CareEventSource.CheckIn,
            $"missed-check-in:{seed.MainElderId:N}:{now.UtcTicks}",
            "演示记录：老人未在计划时间内完成平安确认",
            now,
            "社区照料队列");
        Assert.True(careEvent.TryTransition(
            CareEventStatus.UnableToConfirm,
            CareEventActorKind.Background,
            null,
            "演示升级时限内仍未确认",
            null,
            now.AddMinutes(1)).IsAllowed);
        Assert.True(careEvent.AddContactAttempt(
            Guid.NewGuid(),
            "legacy-phone-attempt",
            ContactAttemptKind.PhoneConfirmation,
            "老人演示电话",
            now.AddMinutes(2),
            "已生成模拟电话确认记录"));
        var legacyEvidence = new[]
        {
            (Kind: "ServiceOrderCreated", Summary: "已创建演示服务工单：助餐服务，今日 12:00"),
            (Kind: "ServiceOrderAccepted", Summary: "服务人员已接收演示工单：助餐服务"),
            (Kind: "VisitScheduled", Summary: "已安排演示探访：2026-08-25 09:00"),
            (Kind: "VisitStarted", Summary: "工作人员已开始演示探访"),
            (Kind: "FollowUpScheduled", Summary: "已安排演示随访：2026-08-26 09:00"),
            (Kind: "FamilyNote", Summary: "家属填写：演示如何处理，请保留原文"),
        };
        for (var index = 0; index < legacyEvidence.Length; index++)
        {
            careEvent.AddEvidence(
                Guid.NewGuid(),
                legacyEvidence[index].Kind,
                legacyEvidence[index].Summary,
                now.AddMinutes(index + 3),
                now.AddMinutes(index + 3),
                $"legacy-evidence-{index}");
        }
        db.CareEvents.Add(careEvent);

        db.AccessAuditRecords.AddRange(
            new AccessAuditRecord(
                Guid.NewGuid(),
                "老人授权演示",
                Guid.NewGuid(),
                seed.MainElderId,
                "授权查看照料资料",
                now,
                "recentStatus"),
            new AccessAuditRecord(
                Guid.NewGuid(),
                "老人撤回授权演示",
                Guid.NewGuid(),
                seed.MainElderId,
                "撤回资料访问授权",
                now,
                "recentStatus"));
        db.AuditEntries.AddRange(
            new AuditEntry(
                Guid.NewGuid(),
                null,
                "System",
                "ResetDemoData",
                "DemoDataset",
                seed.MainElderId,
                now,
                "管理员确认恢复 20 人演示数据",
                null,
                "Ready"),
            new AuditEntry(
                Guid.NewGuid(),
                null,
                "System",
                "Update",
                "ElderProfile",
                seed.MainElderId,
                now,
                "演示业务资料已更新",
                null,
                null));
        await db.SaveChangesAsync();

        foreach (var (elder, index) in seed.Elders.Select((elder, index) => (elder, index)))
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE ElderProfiles SET DemoDisplayName = {"演示·" + elder.DemoDisplayName} WHERE Id = {elder.Id}");
            var contact = elder.EmergencyContacts.Single();
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE EmergencyContacts SET DemoName = {$"演示联系人{index + 1:00}"} WHERE Id = {contact.Id}");
        }
        var untouchedElderId = seed.Elders[19].Id;
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE ElderProfiles SET DemoDisplayName = {"演示·自定义姓名"} WHERE Id = {untouchedElderId}");

        await RuntimeCopyUpgrade.ApplyAsync(db);
        await RuntimeCopyUpgrade.ApplyAsync(db);
        db.ChangeTracker.Clear();

        var upgradedElders = await db.ElderProfiles.OrderBy(item => item.Id).ToListAsync();
        Assert.Equal("演示·自定义姓名", upgradedElders.Single(item => item.Id == untouchedElderId).DemoDisplayName);
        Assert.DoesNotContain(
            upgradedElders.Where(item => item.Id != untouchedElderId),
            item => item.DemoDisplayName.StartsWith("演示·", StringComparison.Ordinal));
        Assert.DoesNotContain(
            await db.EmergencyContacts.ToListAsync(),
            item => item.DemoName.StartsWith("演示联系人", StringComparison.Ordinal));
        Assert.Equal(
            ["复诊预约提醒", "社区活动提醒", "上门探访提醒"],
            await db.Reminders.OrderBy(item => item.Id).Select(item => item.DemoLabel).ToArrayAsync());
        Assert.Equal("客厅 SOS 设备", (await db.Devices.SingleAsync()).DisplayName);
        Assert.Equal(
            "老人未在计划时间内完成平安确认",
            (await db.CareEvents.SingleAsync()).Summary);
        Assert.Equal("升级时限内仍未确认", (await db.CareEventTransitions.SingleAsync()).Reason);
        Assert.Equal("老人模拟电话", (await db.ContactAttempts.SingleAsync()).TargetLabel);
        var evidenceSummaries = (await db.CareEventEvidence.ToListAsync())
            .OrderBy(item => item.OccurredAt)
            .Select(item => item.Summary)
            .ToArray();
        Assert.Equal(
            [
                "已创建服务工单：助餐服务，今日 12:00",
                "服务人员已接收工单：助餐服务",
                "已安排探访：2026-08-25 09:00",
                "工作人员已开始探访",
                "已安排随访：2026-08-26 09:00",
                "家属填写：演示如何处理，请保留原文",
            ],
            evidenceSummaries);
        Assert.Equal(
            ["老人授权", "老人撤回授权"],
            await db.AccessAuditRecords.OrderBy(item => item.Action).Select(item => item.Action).ToArrayAsync());
        Assert.Equal(
            ["管理员确认恢复 20 人初始数据", "业务资料已更新"],
            await db.AuditEntries.OrderBy(item => item.Action).Select(item => item.Reason).ToArrayAsync());
    }
}
