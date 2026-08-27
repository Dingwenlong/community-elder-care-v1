using System.Security.Cryptography;
using System.Text;
using CommunityElderCare.Core.CareEvents;
using CommunityElderCare.Core.CareWork;
using CommunityElderCare.Core.Common;
using CommunityElderCare.Core.Devices;
using CommunityElderCare.Core.Identity;
using CommunityElderCare.Infrastructure.Identity;
using CommunityElderCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityElderCare.Infrastructure.Demo;

public sealed class OperationsScenarioService(CommunityCareDbContext db, TimeProvider clock, DemoMutationGate gate)
{
    public async Task<object> LoadAsync(ActorContext actor, CancellationToken ct)
    {
        using var lease = await gate.EnterAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        if (await db.CareEvents.AnyAsync(e => e.SourceEventId.StartsWith("operations-scenario:v1:"), ct))
            return new { alreadyLoaded = true, eventCount = 12 };
        var mainId = DemoSeedBuilder.Build(20, 20260824, clock.GetUtcNow()).MainElderId;
        var elders = await db.ElderProfiles.Where(e => e.IsDemoData && e.AreaCode == "A01" && e.Id != mainId)
            .OrderBy(e => e.Id).ToListAsync(ct);
        if (elders.Count == 0) throw new InvalidOperationException("缺少运营场景所需的虚构档案。");
        var now = clock.GetUtcNow();
        var deviceId = Id("device");
        var device = await db.Devices.SingleOrDefaultAsync(d => d.Id == deviceId, ct);
        if (device is null)
        {
            device = Device.Register(deviceId, elders[0].Id, "活动室求助按钮（模拟）", null, now.AddDays(-12));
            db.Devices.Add(device);
        }
        for (var i = 0; i < 12; i++)
        {
            var created = now.AddDays(-i).AddHours(-4);
            var staff = i % 2 == 0 ? DemoIdentitySeed.CommunityUserId : DemoIdentitySeed.SecondCommunityUserId;
            var worker = i % 2 == 0 ? DemoIdentitySeed.ServiceWorkerUserId : DemoIdentitySeed.SecondServiceWorkerUserId;
            var elderId = elders[i % elders.Count].Id;
            var evt = CareEvent.Create(Id($"event:{i}"), elderId, CareEventCategory.GeneralService,
                CareEventLevel.GeneralService, CareEventSource.StaffVisit, $"operations-scenario:v1:{i}",
                "居家照料安排", created, "A01:care", created);
            evt.Accept(staff, created.AddMinutes(10));
            var staffActor = new ActorContext(staff, DemoRole.CommunityStaff, null, "A01", null);
            var workerActor = new ActorContext(worker, DemoRole.ServiceWorker, null, "A01", null);
            var due = i == 0 ? now.AddHours(2) : created.AddHours(3);
            var visit = VisitTask.Create(Id($"visit:{i}"), evt.Id, elderId, staff,
                created.AddHours(1), due, false, created.AddMinutes(15)).Value!;
            var order = ServiceOrder.Create(Id($"order:{i}"), evt.Id, elderId, "助餐配送",
                "按预约时间送达", "到达后联系社区", worker, false, created.AddMinutes(15), due).Value!;
            db.VisitTasks.Add(visit);
            db.ServiceOrders.Add(order);
            if (i >= 3)
            {
                evt.TryTransition(CareEventStatus.InProgress, CareEventActorKind.Staff, staff, "开始照料", null, created.AddMinutes(20));
                visit.Start(staffActor, created.AddHours(1));
                visit.Complete(staffActor, "上门记录已确认", "已完成上门关怀", "探访完成", created.AddHours(1.5));
                order.Accept(workerActor, created.AddMinutes(30));
                order.Complete(workerActor, "已完成助餐配送", created.AddHours(1.5));
                evt.TryTransition(CareEventStatus.Resolved, CareEventActorKind.Staff, staff, "本次服务已完成", "完成照料安排", created.AddHours(2));
                var follow = FollowUp.Create(Id($"follow:{i}"), evt.Id, elderId, staff,
                    created.AddHours(3), created.AddHours(2)).Value!;
                evt.SetWorkState(false, true, false);
                evt.TryTransition(CareEventStatus.FollowUpPending, CareEventActorKind.Staff, staff, "安排回访", null, created.AddHours(2));
                follow.Complete(staffActor, "已确认服务结果", created.AddHours(3));
                evt.SetWorkState(false, true, true);
                evt.TryTransition(CareEventStatus.Closed, CareEventActorKind.Staff, staff, "回访完成后结案", null, created.AddHours(3.1));
                db.FollowUps.Add(follow);
            }
            db.CareEvents.Add(evt);
        }
        // Simulation-only device, with signals attached to its own elder's events.
        var linked = db.ChangeTracker.Entries<CareEvent>().Select(e => e.Entity).Where(e => e.ElderId == device.ElderId).ToList();
        foreach (var evt in linked.Take(3))
            db.DeviceSignals.Add(DeviceSignal.Receive(Id($"signal:{evt.Id}"), deviceId, Id($"device-event:{evt.Id}"),
                evt.Id, evt.CreatedAt, evt.CreatedAt, DeviceSignalType.NoWaterActivity, null, true));
        db.AuditEntries.Add(new AuditEntry(Guid.NewGuid(), actor.UserId, actor.Role.ToString(),
            "OperationsScenarioLoaded", "DemoDataset", Id("scenario"), now,
            "管理员确认加载运营演示场景，新增 12 条虚构照料记录", null, "Loaded"));
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new { alreadyLoaded = false, eventCount = 12 };
    }

    private static Guid Id(string key) => new(SHA256.HashData(Encoding.UTF8.GetBytes($"operations-scenario:v1:{key}")).AsSpan(0, 16));
}
