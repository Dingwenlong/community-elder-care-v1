using CommunityElderCare.Core.CareEvents;
using CommunityElderCare.Core.CareWork;
using CommunityElderCare.Core.Elders;
using CommunityElderCare.Core.Identity;
using CommunityElderCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityElderCare.Infrastructure.CareWork;

public sealed record OperationsTask(
    Guid TaskId, string TaskType, Guid CareEventId, Guid ElderId, string ElderDisplayName,
    Guid AssignedUserId, string AssignedDisplayName, string AreaCode, WorkStatus Status,
    DateTimeOffset CreatedAt, DateTimeOffset? DueAt, DateTimeOffset? CompletedAt,
    bool IsMandatory, Guid Version, Guid? EventOwnerUserId, bool IsOverdue);
public sealed record PersonnelSummary(
    Guid UserId, string DisplayName, DemoRole Role, string? AreaCode, int PendingCount, int OverdueCount);

public sealed class OperationsQuery(CommunityCareDbContext db, TimeProvider clock)
{
    public static bool CanRead(ActorContext actor) =>
        actor.Role == DemoRole.Administrator ||
        (actor.Role == DemoRole.CommunityStaff && !string.IsNullOrWhiteSpace(actor.AreaCode));

    public IQueryable<ElderProfile> Elders(ActorContext actor, string? areaCode = null) =>
        db.ElderProfiles.AsNoTracking().Where(e =>
            (actor.Role == DemoRole.Administrator || e.AreaCode == actor.AreaCode) &&
            (areaCode == null || e.AreaCode == areaCode));

    public async Task<List<OperationsTask>> TasksAsync(ActorContext actor, CancellationToken ct, string? areaCode = null)
    {
        var elders = await Elders(actor, areaCode).ToDictionaryAsync(e => e.Id, ct);
        var ids = elders.Keys.ToArray();
        var events = await db.CareEvents.AsNoTracking().Where(e => ids.Contains(e.ElderId))
            .Select(e => new { e.Id, e.CurrentOwnerUserId }).ToDictionaryAsync(e => e.Id, ct);
        var people = await db.UserAccounts.AsNoTracking()
            .Where(u => u.Role == DemoRole.CommunityStaff || u.Role == DemoRole.ServiceWorker)
            .ToDictionaryAsync(u => u.Id, ct);
        var visits = await db.VisitTasks.AsNoTracking().Where(t => ids.Contains(t.ElderId)).ToListAsync(ct);
        var orders = await db.ServiceOrders.AsNoTracking().Where(t => ids.Contains(t.ElderId)).ToListAsync(ct);
        var followUps = await db.FollowUps.AsNoTracking().Where(t => ids.Contains(t.ElderId)).ToListAsync(ct);
        var result = new List<OperationsTask>();
        foreach (var t in visits) Add(t, "Visit", t.CreatedAt, t.ScheduledEndAt, t.CompletedAt, t.IsMandatory);
        foreach (var t in orders) Add(t, "ServiceOrder", t.CreatedAt, t.DueAt, t.CompletedAt, t.IsMandatory);
        foreach (var t in followUps) Add(t, "FollowUp", t.CreatedAt, t.DueAt, t.CompletedAt, t.IsMandatory);
        return result.OrderByDescending(t => t.IsOverdue).ThenBy(t => t.DueAt ?? DateTimeOffset.MaxValue)
            .ThenBy(t => t.TaskId).ToList();

        void Add(IAssignableCareTask t, string type, DateTimeOffset created, DateTimeOffset? due,
            DateTimeOffset? completed, bool mandatory)
        {
            var elder = elders[t.ElderId];
            var name = people.TryGetValue(t.AssignedUserId, out var user) ? user.DisplayName : "未找到人员";
            result.Add(new(t.Id, type, t.CareEventId, t.ElderId, elder.DemoDisplayName,
                t.AssignedUserId, name, elder.AreaCode, t.Status, created, due, completed, mandatory,
                t.Version, events.GetValueOrDefault(t.CareEventId)?.CurrentOwnerUserId,
                t.Status is not WorkStatus.Completed and not WorkStatus.Cancelled && due < clock.GetUtcNow()));
        }
    }

    public async Task<List<PersonnelSummary>> PersonnelAsync(ActorContext actor, CancellationToken ct, string? areaCode = null)
    {
        var tasks = await TasksAsync(actor, ct, areaCode);
        var people = await db.UserAccounts.AsNoTracking().Where(u =>
            (u.Role == DemoRole.CommunityStaff || u.Role == DemoRole.ServiceWorker) &&
            (actor.Role == DemoRole.Administrator || u.AreaCode == actor.AreaCode) &&
            (areaCode == null || u.AreaCode == areaCode)).ToListAsync(ct);
        return people.OrderBy(u => u.Role).ThenBy(u => u.DisplayName).Select(u => new PersonnelSummary(
            u.Id, u.DisplayName, u.Role, u.AreaCode,
            tasks.Count(t => t.AssignedUserId == u.Id && t.Status is not WorkStatus.Completed and not WorkStatus.Cancelled),
            tasks.Count(t => t.AssignedUserId == u.Id && t.IsOverdue))).ToList();
    }
}
