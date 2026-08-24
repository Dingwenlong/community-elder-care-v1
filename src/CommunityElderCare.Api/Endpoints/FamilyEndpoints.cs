using CommunityElderCare.Api.Contracts.Family;
using CommunityElderCare.Api.Identity;
using CommunityElderCare.Core.CareEvents;
using CommunityElderCare.Core.Identity;
using CommunityElderCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityElderCare.Api.Endpoints;

public static class FamilyEndpoints
{
    public static IEndpointRouteBuilder MapFamilyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/family/elders/{elderId:guid}")
            .RequireAuthorization();
        group.MapGet("/summary", GetSummaryAsync);
        group.MapGet("/care-records", GetCareRecordsAsync);
        return endpoints;
    }

    private static async Task<IResult> GetSummaryAsync(
        Guid elderId,
        HttpContext httpContext,
        CommunityCareDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (!IsScopedFamily(httpContext, elderId))
        {
            return ForbiddenScope();
        }
        var scope = await GetScopeAsync(
            elderId,
            httpContext,
            dbContext,
            timeProvider.GetUtcNow(),
            cancellationToken);
        if (scope is null)
        {
            return ConsentRequired();
        }

        var elderName = await dbContext.ElderProfiles
            .AsNoTracking()
            .Where(elder => elder.Id == elderId)
            .Select(elder => elder.DemoDisplayName)
            .SingleOrDefaultAsync(cancellationToken);
        if (elderName is null)
        {
            return Results.NotFound();
        }

        string? recentStatus = null;
        if (scope.Fields.Contains(ConsentField.RecentStatus))
        {
            var checkInTimes = await dbContext.CheckIns
                .AsNoTracking()
                .Where(checkIn => checkIn.ElderId == elderId)
                .Select(checkIn => checkIn.ReceivedAt)
                .ToListAsync(cancellationToken);
            var latestCheckIn = checkInTimes
                .OrderByDescending(value => value)
                .Cast<DateTimeOffset?>()
                .FirstOrDefault();
            recentStatus = latestCheckIn is null
                ? "今日尚未收到平安确认"
                : $"今天 {latestCheckIn:HH:mm} 已完成平安确认";
        }

        string? reminderSummary = null;
        if (scope.Fields.Contains(ConsentField.ReminderCompletion))
        {
            var reminders = await dbContext.Reminders
                .AsNoTracking()
                .Where(reminder => reminder.ElderId == elderId)
                .Select(reminder => reminder.CompletedAt)
                .ToListAsync(cancellationToken);
            reminderSummary = $"今日已完成 {reminders.Count(value => value is not null)}/{reminders.Count} 项提醒";
        }

        string? careProgress = null;
        string? lastCommunityConfirmation = null;
        if (scope.Fields.Contains(ConsentField.CareEventSummary))
        {
            var eventCandidates = await dbContext.CareEvents
                .AsNoTracking()
                .Where(careEvent => careEvent.ElderId == elderId)
                .Select(careEvent => new { careEvent.Status, careEvent.LastActivityAt })
                .ToListAsync(cancellationToken);
            var latestEvent = eventCandidates
                .OrderByDescending(careEvent => careEvent.LastActivityAt)
                .FirstOrDefault();
            careProgress = latestEvent is null
                ? "当前没有待确认事件"
                : NaturalEventStatus(latestEvent.Status);
            if (latestEvent is not null)
            {
                lastCommunityConfirmation =
                    $"{latestEvent.LastActivityAt:MM-dd HH:mm} 社区已记录确认进展";
            }
        }

        string? visitSummary = null;
        if (scope.Fields.Contains(ConsentField.VisitSummary))
        {
            var visitCandidates = await dbContext.VisitTasks
                .AsNoTracking()
                .Where(visit => visit.ElderId == elderId && visit.ConfirmedSummary != null)
                .Select(visit => new { visit.CompletedAt, visit.ConfirmedSummary })
                .ToListAsync(cancellationToken);
            visitSummary = visitCandidates
                .OrderByDescending(visit => visit.CompletedAt)
                .Select(visit => visit.ConfirmedSummary)
                .FirstOrDefault()
                ?? "暂无已授权探访摘要";
        }

        return Results.Ok(new FamilySummaryResponse(
            elderName,
            scope.Fields.Order().ToList(),
            scope.ExpiresAt,
            recentStatus,
            reminderSummary,
            careProgress,
            visitSummary,
            lastCommunityConfirmation));
    }

    private static async Task<IResult> GetCareRecordsAsync(
        Guid elderId,
        HttpContext httpContext,
        CommunityCareDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (!IsScopedFamily(httpContext, elderId))
        {
            return ForbiddenScope();
        }
        var scope = await GetScopeAsync(
            elderId,
            httpContext,
            dbContext,
            timeProvider.GetUtcNow(),
            cancellationToken);
        if (scope is null || !scope.Fields.Contains(ConsentField.VisitSummary))
        {
            return ConsentRequired();
        }

        var visits = await dbContext.VisitTasks
            .AsNoTracking()
            .Where(visit =>
                visit.ElderId == elderId &&
                visit.CompletedAt != null &&
                visit.ConfirmedSummary != null)
            .Select(visit => new FamilyCareRecordResponse(
                visit.CompletedAt!.Value,
                "Visit",
                visit.ConfirmedSummary!,
                true))
            .ToListAsync(cancellationToken);
        var orders = await dbContext.ServiceOrders
            .AsNoTracking()
            .Where(order =>
                order.ElderId == elderId &&
                order.CompletedAt != null &&
                order.Result != null)
            .Select(order => new FamilyCareRecordResponse(
                order.CompletedAt!.Value,
                "ServiceOrder",
                order.Result!,
                true))
            .ToListAsync(cancellationToken);
        var followUps = await dbContext.FollowUps
            .AsNoTracking()
            .Where(followUp =>
                followUp.ElderId == elderId &&
                followUp.CompletedAt != null &&
                followUp.Result != null)
            .Select(followUp => new FamilyCareRecordResponse(
                followUp.CompletedAt!.Value,
                "FollowUp",
                followUp.Result!,
                true))
            .ToListAsync(cancellationToken);

        return Results.Ok(visits
            .Concat(orders)
            .Concat(followUps)
            .OrderByDescending(record => record.OccurredAt)
            .ToList());
    }

    private static async Task<FamilyScope?> GetScopeAsync(
        Guid elderId,
        HttpContext httpContext,
        CommunityCareDbContext dbContext,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var actor = httpContext.User.GetActorContext();
        if (actor.Role != DemoRole.Family || actor.ElderId != elderId)
        {
            return null;
        }

        var candidates = await dbContext.ConsentGrants
            .AsNoTracking()
            .Include(candidate => candidate.Fields)
            .Where(candidate =>
                candidate.ElderId == elderId &&
                candidate.GranteeUserId == actor.UserId &&
                candidate.RevokedAt == null)
            .ToListAsync(cancellationToken);
        var grant = candidates
            .Where(candidate => candidate.ExpiresAt > now)
            .OrderByDescending(candidate => candidate.GrantedAt)
            .FirstOrDefault();
        return grant is null
            ? null
            : new FamilyScope(
                grant.Fields.Select(field => field.Field).ToHashSet(),
                grant.ExpiresAt);
    }

    private static string NaturalEventStatus(CareEventStatus status) => status switch
    {
        CareEventStatus.PendingConfirmation => "社区正在电话确认",
        CareEventStatus.FollowUpPending => "已安排次日回访",
        CareEventStatus.Closed => "本次照料已完成",
        _ => "社区正在跟进",
    };

    private static bool IsScopedFamily(HttpContext httpContext, Guid elderId)
    {
        var actor = httpContext.User.GetActorContext();
        return actor.Role == DemoRole.Family && actor.ElderId == elderId;
    }

    private static IResult ForbiddenScope() => Results.Problem(
        statusCode: StatusCodes.Status403Forbidden,
        title: "Family scope is required",
        extensions: new Dictionary<string, object?> { ["code"] = "FORBIDDEN_SCOPE" });

    private static IResult ConsentRequired() => Results.Problem(
        statusCode: StatusCodes.Status403Forbidden,
        title: "Family consent is required",
        extensions: new Dictionary<string, object?> { ["code"] = "CONSENT_REQUIRED" });

    private sealed record FamilyScope(HashSet<ConsentField> Fields, DateTimeOffset ExpiresAt);
}
