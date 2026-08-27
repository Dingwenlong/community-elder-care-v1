using CommunityElderCare.Core.CareEvents;
using CommunityElderCare.Core.CareWork;
using CommunityElderCare.Core.Common;
using CommunityElderCare.Core.Identity;
using CommunityElderCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityElderCare.Infrastructure.CareWork;

public sealed class TaskAssignmentService(CommunityCareDbContext db, TimeProvider clock)
{
    public async Task<OperationResult<bool>> ReassignAsync(string type, Guid id, Guid assignee,
        string reason, Guid expectedVersion, ActorContext actor, CancellationToken ct)
    {
        if (actor.Role != DemoRole.CommunityStaff) return Fail("FORBIDDEN_SCOPE", "仅事件负责人可以转派。");
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 512)
            return Fail("REASON_REQUIRED", "请填写不超过 512 字的转派原因。");
        IAssignableCareTask? task = type switch
        {
            "Visit" => await db.VisitTasks.SingleOrDefaultAsync(t => t.Id == id, ct),
            "ServiceOrder" => await db.ServiceOrders.SingleOrDefaultAsync(t => t.Id == id, ct),
            "FollowUp" => await db.FollowUps.SingleOrDefaultAsync(t => t.Id == id, ct),
            _ => null,
        };
        if (task is null) return Fail("NOT_FOUND", "任务不存在。");
        var evt = await db.CareEvents.Include(e => e.Evidence).SingleAsync(e => e.Id == task.CareEventId, ct);
        var area = await db.ElderProfiles.Where(e => e.Id == task.ElderId).Select(e => e.AreaCode).SingleAsync(ct);
        if (evt.CurrentOwnerUserId != actor.UserId || area != actor.AreaCode)
            return Fail("FORBIDDEN_SCOPE", "仅本片区的事件负责人可以转派。");
        if (task.Version != expectedVersion) return Fail("CONCURRENT_CHANGE", "任务已更新，请刷新后重试。");
        if (task.Status != WorkStatus.Assigned || CareEventStateMachine.IsTerminal(evt.Status))
            return Fail("INVALID_WORK_STATUS", "只能转派未开始的任务。");
        var role = type == "ServiceOrder" ? DemoRole.ServiceWorker : DemoRole.CommunityStaff;
        var person = await db.UserAccounts.SingleOrDefaultAsync(u => u.Id == assignee && u.Role == role && u.AreaCode == area, ct);
        if (person is null || assignee == task.AssignedUserId)
            return Fail("INVALID_ASSIGNEE", "请选择同片区的另一位合资格人员。");
        var from = task.AssignedUserId;
        var now = clock.GetUtcNow();
        task.Reassign(assignee);
        var change = new TaskReassignment
        {
            Id = Guid.NewGuid(), TaskType = type, TaskId = id, CareEventId = evt.Id,
            FromUserId = from, ToUserId = assignee, ActorUserId = actor.UserId,
            Reason = reason.Trim(), OccurredAt = now,
        };
        db.TaskReassignments.Add(change);
        var evidenceId = Guid.NewGuid();
        evt.AddEvidence(evidenceId, "TaskReassigned", $"任务已转派给{person.DisplayName}：{reason.Trim()}",
            now, now, $"reassignment:{change.Id:N}");
        db.CareEventEvidence.Add(evt.Evidence.Single(e => e.Id == evidenceId));
        db.AuditEntries.Add(new AuditEntry(Guid.NewGuid(), actor.UserId, actor.Role.ToString(),
            "TaskReassigned", type, id, now, reason.Trim(), from.ToString(), assignee.ToString()));
        await db.SaveChangesAsync(ct);
        return new(true, true, null, null);
    }

    private static OperationResult<bool> Fail(string code, string message) => new(false, false, code, message);
}
