using CommunityElderCare.Core.CareEvents;
using CommunityElderCare.Core.CareWork;
using CommunityElderCare.Core.Identity;

namespace CommunityElderCare.UnitTests.CareWork;

public sealed class CareWorkTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 24, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid ElderId = Guid.Parse("11111111-1111-1111-1111-111111111101");
    private static readonly Guid EventId = Guid.Parse("55555555-5555-5555-5555-555555555501");
    private static readonly Guid ServiceWorkerId = Guid.Parse("22222222-2222-2222-2222-222222222204");

    [Fact]
    public void Service_worker_can_only_complete_the_assigned_order()
    {
        var order = ServiceOrder.Create(
            Guid.NewGuid(),
            EventId,
            ElderId,
            "助餐配送",
            "10:00-11:00",
            "到达门口后按演示流程联系",
            ServiceWorkerId,
            isMandatory: true,
            Now).Value!;
        var otherWorker = WorkerActor(Guid.NewGuid(), order.Id);
        var assignedWorker = WorkerActor(ServiceWorkerId, order.Id);

        var denied = order.Complete(otherWorker, "已完成送餐", Now.AddMinutes(30));
        var allowed = order.Complete(assignedWorker, "已完成送餐", Now.AddMinutes(30));

        Assert.Equal("FORBIDDEN_SCOPE", denied.ErrorCode);
        Assert.True(allowed.IsSuccess);
        Assert.Equal(WorkStatus.Completed, order.Status);
    }

    [Fact]
    public void Visit_must_start_before_it_can_complete()
    {
        var staffId = Guid.NewGuid();
        var visit = VisitTask.Create(
            Guid.NewGuid(),
            EventId,
            ElderId,
            staffId,
            Now.AddHours(1),
            Now.AddHours(2),
            isMandatory: true,
            Now).Value!;
        var actor = StaffActor(staffId);

        var tooEarly = visit.Complete(
            actor,
            "演示原始记录",
            "老人状态已确认",
            "完成",
            Now.AddHours(1));
        var started = visit.Start(actor, Now.AddHours(1));
        var completed = visit.Complete(
            actor,
            "演示原始记录",
            "老人状态已确认",
            "完成",
            Now.AddHours(1).AddMinutes(20));

        Assert.Equal("INVALID_WORK_STATUS", tooEarly.ErrorCode);
        Assert.True(started.IsSuccess);
        Assert.True(completed.IsSuccess);
        Assert.Equal(WorkStatus.Completed, visit.Status);
    }

    [Fact]
    public void Cancellation_requires_a_reason()
    {
        var staffId = Guid.NewGuid();
        var visit = VisitTask.Create(
            Guid.NewGuid(),
            EventId,
            ElderId,
            staffId,
            Now.AddHours(1),
            Now.AddHours(2),
            isMandatory: false,
            Now).Value!;
        var actor = StaffActor(staffId);

        var denied = visit.Cancel(actor, " ", Now);
        var allowed = visit.Cancel(actor, "老人已改约演示时间", Now);

        Assert.Equal("REASON_REQUIRED", denied.ErrorCode);
        Assert.True(allowed.IsSuccess);
        Assert.Equal("老人已改约演示时间", visit.CancellationReason);
    }

    [Fact]
    public void Follow_up_due_time_must_be_in_the_future()
    {
        var assignedStaffId = Guid.NewGuid();

        var invalid = FollowUp.Create(
            Guid.NewGuid(),
            EventId,
            ElderId,
            assignedStaffId,
            Now,
            Now);
        var valid = FollowUp.Create(
            Guid.NewGuid(),
            EventId,
            ElderId,
            assignedStaffId,
            Now.AddDays(1),
            Now);

        Assert.Equal("INVALID_DUE_TIME", invalid.ErrorCode);
        Assert.True(valid.IsSuccess);
        Assert.Equal(Now.AddDays(1), valid.Value!.DueAt);
    }

    [Fact]
    public void Event_cannot_close_while_mandatory_work_is_unfinished()
    {
        var staffId = Guid.NewGuid();
        var careEvent = CareEvent.Create(
            EventId,
            ElderId,
            CareEventCategory.SafetyHealth,
            CareEventLevel.NeedsConfirmation,
            CareEventSource.StaffVisit,
            "staff:work-test",
            "演示照料事件",
            Now,
            "A01:care");
        Assert.True(careEvent.Accept(staffId, Now).IsAllowed);
        Assert.True(careEvent.TryTransition(
            CareEventStatus.InProgress,
            CareEventActorKind.Staff,
            staffId,
            reason: "开始处理",
            resolution: null,
            Now).IsAllowed);
        Assert.True(careEvent.TryTransition(
            CareEventStatus.Resolved,
            CareEventActorKind.Staff,
            staffId,
            reason: "处理完成",
            resolution: "已完成现场确认",
            Now).IsAllowed);
        careEvent.SetWorkState(
            hasIncompleteMandatoryTask: true,
            requiresFollowUp: false,
            isFollowUpCompleted: false);

        var close = careEvent.TryTransition(
            CareEventStatus.Closed,
            CareEventActorKind.Staff,
            staffId,
            reason: "申请结案",
            resolution: null,
            Now);

        Assert.False(close.IsAllowed);
        Assert.Equal("MANDATORY_TASK_INCOMPLETE", close.ErrorCode);
    }

    private static ActorContext StaffActor(Guid userId) =>
        new(userId, DemoRole.CommunityStaff, null, "A01", null);

    private static ActorContext WorkerActor(Guid userId, Guid orderId) =>
        new(userId, DemoRole.ServiceWorker, ElderId, null, orderId);
}
