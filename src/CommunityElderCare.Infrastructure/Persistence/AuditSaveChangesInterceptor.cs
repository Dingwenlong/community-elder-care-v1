using System.IdentityModel.Tokens.Jwt;
using CommunityElderCare.Core.CareEvents;
using CommunityElderCare.Core.CareWork;
using CommunityElderCare.Core.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CommunityElderCare.Infrastructure.Persistence;

public sealed class AuditSaveChangesInterceptor(
    IHttpContextAccessor httpContextAccessor,
    TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AddAuditEntries(eventData.Context);
        return ValueTask.FromResult(result);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        AddAuditEntries(eventData.Context);
        return result;
    }

    private void AddAuditEntries(DbContext? dbContext)
    {
        if (dbContext is null)
        {
            return;
        }

        var actor = ResolveActor();
        var now = timeProvider.GetUtcNow();
        var changed = dbContext.ChangeTracker.Entries()
            .Where(entry => entry.Entity is not AuditEntry &&
                entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();
        foreach (var entry in changed)
        {
            var descriptor = Describe(entry);
            if (descriptor is null)
            {
                continue;
            }
            dbContext.Set<AuditEntry>().Add(new AuditEntry(
                Guid.NewGuid(),
                actor.UserId,
                actor.Kind,
                descriptor.Action,
                descriptor.EntityType,
                descriptor.EntityId,
                now,
                descriptor.Reason,
                descriptor.BeforeStatus,
                descriptor.AfterStatus));
        }
    }

    private AuditActor ResolveActor()
    {
        var context = httpContextAccessor.HttpContext;
        var principal = context?.User;
        if (principal?.Identity?.IsAuthenticated == true)
        {
            Guid? userId = Guid.TryParse(
                principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value,
                out var parsed)
                ? parsed
                : null;
            return new AuditActor(userId, principal.FindFirst("role")?.Value ?? "AuthenticatedUser");
        }
        if (context?.Request.Headers.ContainsKey("X-Device-Token") == true)
        {
            return new AuditActor(null, "Device");
        }
        return new AuditActor(null, "Background");
    }

    private static AuditDescriptor? Describe(EntityEntry entry)
    {
        if (entry.Entity is CareEvent careEvent && entry.State == EntityState.Added)
        {
            return new("CareEventCreated", "CareEvent", careEvent.Id, "照料事件已创建", null, careEvent.Status.ToString());
        }
        if (entry.Entity is CareEventEvidence evidence && entry.State == EntityState.Added)
        {
            return new("EvidenceMerged", "CareEvent", evidence.CareEventId, "事件证据已合并", null, null);
        }
        if (entry.Entity is CareEventTransition transition && entry.State == EntityState.Added)
        {
            return new(
                TransitionAction(transition.ToStatus),
                "CareEvent",
                transition.CareEventId,
                string.IsNullOrWhiteSpace(transition.Reason) ? "事件状态已更新" : transition.Reason,
                transition.FromStatus.ToString(),
                transition.ToStatus.ToString());
        }
        if (entry.Entity is ContactAttempt contact && entry.State == EntityState.Added)
        {
            return new("SimulationContactRecorded", "CareEvent", contact.CareEventId, "模拟联系动作已记录", null, null);
        }
        if (entry.Entity is VisitTask visit)
        {
            return entry.State switch
            {
                EntityState.Added => new("VisitScheduled", "VisitTask", visit.Id, "探访任务已安排", null, visit.Status.ToString()),
                EntityState.Modified when visit.Status == WorkStatus.Completed => new(
                    "VisitCompleted", "VisitTask", visit.Id, "探访结果已确认", OriginalStatus(entry), visit.Status.ToString()),
                EntityState.Modified => new(
                    "VisitUpdated", "VisitTask", visit.Id, "探访任务状态已更新", OriginalStatus(entry), visit.Status.ToString()),
                _ => null,
            };
        }
        if (entry.Entity is FollowUp followUp)
        {
            return entry.State switch
            {
                EntityState.Added => new("FollowUpScheduled", "FollowUp", followUp.Id, "随访任务已安排", null, followUp.Status.ToString()),
                EntityState.Modified when followUp.Status == WorkStatus.Completed => new(
                    "FollowUpCompleted", "FollowUp", followUp.Id, "随访结果已确认", OriginalStatus(entry), followUp.Status.ToString()),
                EntityState.Modified => new(
                    "FollowUpUpdated", "FollowUp", followUp.Id, "随访任务状态已更新", OriginalStatus(entry), followUp.Status.ToString()),
                _ => null,
            };
        }
        if (entry.Entity is ServiceOrder order)
        {
            return new(
                entry.State == EntityState.Added ? "ServiceOrderCreated" : "ServiceOrderUpdated",
                "ServiceOrder",
                order.Id,
                "服务工单已更新",
                entry.State == EntityState.Modified ? OriginalStatus(entry) : null,
                order.Status.ToString());
        }
        if (entry.Entity is NotificationAttempt notification && entry.State == EntityState.Added)
        {
            return new(
                "NotificationAttemptRecorded",
                "CareEvent",
                notification.CareEventId,
                "模拟通知尝试已记录",
                null,
                notification.Outcome);
        }
        if (entry.Entity is BackgroundJobRun jobRun)
        {
            return new(
                entry.State == EntityState.Added ? "BackgroundJobStarted" : "BackgroundJobCompleted",
                "BackgroundJobRun",
                jobRun.Id,
                "后台任务运行状态已记录",
                entry.State == EntityState.Modified ? OriginalStatus(entry, "Result") : null,
                jobRun.Result.ToString());
        }

        var entityId = TryEntityId(entry);
        if (entityId == Guid.Empty)
        {
            return null;
        }
        var action = entry.State switch
        {
            EntityState.Added => "EntityCreated",
            EntityState.Modified => "EntityUpdated",
            EntityState.Deleted => "EntityDeleted",
            _ => "EntityChanged",
        };
        return new(
            action,
            entry.Metadata.ClrType.Name,
            entityId,
            "业务资料已更新",
            null,
            null);
    }

    private static string TransitionAction(CareEventStatus status) => status switch
    {
        CareEventStatus.Accepted => "EventAccepted",
        CareEventStatus.Resolved => "EventResolved",
        CareEventStatus.Closed => "EventClosed",
        CareEventStatus.UnableToConfirm => "EmergencyEscalated",
        _ => "EventStatusChanged",
    };

    private static string? OriginalStatus(EntityEntry entry, string propertyName = "Status") =>
        entry.Metadata.FindProperty(propertyName) is null
            ? null
            : entry.OriginalValues[propertyName]?.ToString();

    private static Guid TryEntityId(EntityEntry entry)
    {
        var idProperty = entry.Metadata.FindProperty("Id");
        return idProperty is not null && entry.Property("Id").CurrentValue is Guid id ? id : Guid.Empty;
    }

    private sealed record AuditActor(Guid? UserId, string Kind);

    private sealed record AuditDescriptor(
        string Action,
        string EntityType,
        Guid EntityId,
        string Reason,
        string? BeforeStatus,
        string? AfterStatus);
}
