using CommunityElderCare.Core.CareEvents;
using CommunityElderCare.Core.Devices;
using Microsoft.EntityFrameworkCore;

namespace CommunityElderCare.Infrastructure.Persistence;

public static class RuntimeCopyUpgrade
{
    private const int Seed = 20260824;

    public static async Task ApplyAsync(
        CommunityCareDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        var seed = DemoSeedBuilder.Build(25, Seed, DateTimeOffset.UnixEpoch);
        foreach (var (elder, index) in seed.Elders.Select((elder, index) => (elder, index)))
        {
            var oldName = $"演示·{elder.DemoDisplayName}";
            var currentName = elder.DemoDisplayName;
            await dbContext.ElderProfiles
                .Where(item =>
                    item.Id == elder.Id &&
                    item.IsDemoData &&
                    item.DemoDisplayName == oldName)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(item => item.DemoDisplayName, currentName),
                    cancellationToken);

            var contact = elder.EmergencyContacts.Single();
            var oldContactName = $"演示联系人{index + 1:00}";
            var currentContactName = contact.DemoName;
            await dbContext.EmergencyContacts
                .Where(item => item.Id == contact.Id && item.DemoName == oldContactName)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(item => item.DemoName, currentContactName),
                    cancellationToken);
        }

        await UpdateReminderAsync(
            dbContext,
            "44444444-4444-4444-4444-444444444402",
            "演示复诊预约提醒",
            "复诊预约提醒",
            cancellationToken);
        await UpdateReminderAsync(
            dbContext,
            "44444444-4444-4444-4444-444444444403",
            "社区活动演示提醒",
            "社区活动提醒",
            cancellationToken);
        await UpdateReminderAsync(
            dbContext,
            "44444444-4444-4444-4444-444444444404",
            "上门探访演示提醒",
            "上门探访提醒",
            cancellationToken);

        await dbContext.Devices
            .Where(item =>
                item.Id == DemoDeviceIds.MainSosDevice &&
                item.IsDemoData &&
                item.DisplayName == "客厅 SOS 演示设备")
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(item => item.DisplayName, "客厅 SOS 设备"),
                cancellationToken);

        var seedElderIds = seed.Elders.Select(item => item.Id).ToArray();
        await dbContext.CareEvents
            .Where(item =>
                seedElderIds.Contains(item.ElderId) &&
                item.IsDemoData &&
                item.Source == CareEventSource.CheckIn &&
                item.SourceEventId.StartsWith("missed-check-in:") &&
                item.Summary == "演示记录：老人未在计划时间内完成平安确认")
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    item => item.Summary,
                    "老人未在计划时间内完成平安确认"),
                cancellationToken);

        await dbContext.CareEventTransitions
            .Where(item =>
                item.IsSimulation &&
                item.ActorKind == CareEventActorKind.Background &&
                item.ToStatus == CareEventStatus.UnableToConfirm &&
                item.Reason == "演示升级时限内仍未确认")
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(item => item.Reason, "升级时限内仍未确认"),
                cancellationToken);

        await dbContext.ContactAttempts
            .Where(item =>
                item.IsSimulation &&
                item.Kind == ContactAttemptKind.PhoneConfirmation &&
                item.TargetLabel == "老人演示电话")
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(item => item.TargetLabel, "老人模拟电话"),
                cancellationToken);

        await UpdateEvidencePrefixAsync(
            dbContext,
            "ServiceOrderCreated",
            "已创建演示服务工单：",
            "已创建服务工单：",
            cancellationToken);
        await UpdateEvidencePrefixAsync(
            dbContext,
            "ServiceOrderAccepted",
            "服务人员已接收演示工单：",
            "服务人员已接收工单：",
            cancellationToken);
        await UpdateEvidencePrefixAsync(
            dbContext,
            "VisitScheduled",
            "已安排演示探访：",
            "已安排探访：",
            cancellationToken);
        await UpdateEvidenceExactAsync(
            dbContext,
            "VisitStarted",
            "工作人员已开始演示探访",
            "工作人员已开始探访",
            cancellationToken);
        await UpdateEvidencePrefixAsync(
            dbContext,
            "FollowUpScheduled",
            "已安排演示随访：",
            "已安排随访：",
            cancellationToken);

        await dbContext.AccessAuditRecords
            .Where(item =>
                seedElderIds.Contains(item.ElderId) &&
                item.IsDemoData &&
                item.Action == "老人授权演示")
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(item => item.Action, "老人授权"),
                cancellationToken);
        await dbContext.AccessAuditRecords
            .Where(item =>
                seedElderIds.Contains(item.ElderId) &&
                item.IsDemoData &&
                item.Action == "老人撤回授权演示")
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(item => item.Action, "老人撤回授权"),
                cancellationToken);

        await dbContext.AuditEntries
            .Where(item =>
                item.IsDemoData &&
                item.Action == "ResetDemoData" &&
                item.EntityType == "DemoDataset" &&
                item.Reason == "管理员确认恢复 20 人演示数据")
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    item => item.Reason,
                    "管理员确认恢复 20 人初始数据"),
                cancellationToken);
        await dbContext.AuditEntries
            .Where(item => item.IsDemoData && item.Reason == "演示业务资料已更新")
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(item => item.Reason, "业务资料已更新"),
                cancellationToken);
    }

    private static Task<int> UpdateReminderAsync(
        CommunityCareDbContext dbContext,
        string reminderId,
        string oldLabel,
        string currentLabel,
        CancellationToken cancellationToken) =>
        dbContext.Reminders
            .Where(item =>
                item.Id == Guid.Parse(reminderId) &&
                item.IsDemoData &&
                item.DemoLabel == oldLabel)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(item => item.DemoLabel, currentLabel),
                cancellationToken);

    private static Task<int> UpdateEvidencePrefixAsync(
        CommunityCareDbContext dbContext,
        string kind,
        string oldPrefix,
        string currentPrefix,
        CancellationToken cancellationToken) =>
        dbContext.CareEventEvidence
            .Where(item =>
                item.IsSimulation &&
                item.Kind == kind &&
                item.Summary.StartsWith(oldPrefix))
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    item => item.Summary,
                    item => currentPrefix + item.Summary.Substring(oldPrefix.Length)),
                cancellationToken);

    private static Task<int> UpdateEvidenceExactAsync(
        CommunityCareDbContext dbContext,
        string kind,
        string oldSummary,
        string currentSummary,
        CancellationToken cancellationToken) =>
        dbContext.CareEventEvidence
            .Where(item =>
                item.IsSimulation &&
                item.Kind == kind &&
                item.Summary == oldSummary)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(item => item.Summary, currentSummary),
                cancellationToken);
}
