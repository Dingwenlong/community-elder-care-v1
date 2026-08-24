using CommunityElderCare.Api.Contracts.CheckIns;
using CommunityElderCare.Api.Identity;
using CommunityElderCare.Core.CheckIns;
using CommunityElderCare.Core.Common;
using CommunityElderCare.Core.Identity;

namespace CommunityElderCare.Api.Endpoints;

public static class CheckInEndpoints
{
    public static IEndpointRouteBuilder MapCheckInEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/elders/{elderId:guid}/check-ins", RecordAsync)
            .RequireAuthorization();
        endpoints.MapGet("/api/v1/elders/{elderId:guid}/today", GetTodayAsync)
            .RequireAuthorization();
        endpoints.MapPost("/api/v1/reminders/{reminderId:guid}/complete", CompleteReminderAsync)
            .RequireAuthorization();
        endpoints.MapPost("/api/v1/reminders/{reminderId:guid}/snooze", SnoozeReminderAsync)
            .RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> RecordAsync(
        Guid elderId,
        string? reason,
        RecordCheckInRequest request,
        HttpContext httpContext,
        ICheckInService service,
        CancellationToken cancellationToken)
    {
        var actor = httpContext.User.GetActorContext();
        var result = actor.Role == DemoRole.CommunityStaff
            ? await service.RecordManualAsync(
                elderId,
                request.RequestId,
                request.ClientTime,
                reason ?? string.Empty,
                actor,
                cancellationToken)
            : await service.RecordAsync(
                elderId,
                request.RequestId,
                request.ClientTime,
                actor,
                cancellationToken);

        return result.IsSuccess ? Results.Ok(ToResponse(result.Value!)) : ToProblem(result);
    }

    private static async Task<IResult> GetTodayAsync(
        Guid elderId,
        HttpContext httpContext,
        ICheckInService service,
        IAccessPolicy accessPolicy,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var actor = httpContext.User.GetActorContext();
        var canReadRecent = await accessPolicy.CanReadAsync(
            actor, elderId, ConsentField.RecentStatus, cancellationToken);
        var canReadReminders = await accessPolicy.CanReadAsync(
            actor, elderId, ConsentField.ReminderCompletion, cancellationToken);
        if (!canReadRecent && !canReadReminders)
        {
            var code = actor.Role == DemoRole.Family ? "CONSENT_REQUIRED" : "FORBIDDEN_SCOPE";
            return Problem(StatusCodes.Status403Forbidden, code, "Access denied");
        }

        var snapshot = await service.GetTodayAsync(elderId, timeProvider.GetUtcNow(), cancellationToken);
        var checkIns = canReadRecent
            ? snapshot.CheckIns.Select(checkIn => new TodayCheckInResponse(
                checkIn.Id,
                checkIn.RequestId,
                checkIn.ClientTime,
                checkIn.ReceivedAt,
                checkIn.Kind.ToString())).ToList()
            : [];
        var reminders = canReadReminders
            ? snapshot.Reminders.Select(reminder => new TodayReminderResponse(
                reminder.Id,
                reminder.Type.ToString(),
                reminder.DemoLabel,
                reminder.DueAt,
                reminder.NextDueAt,
                ReminderState(reminder),
                reminder.CompletedAt,
                reminder.SnoozedAt)).ToList()
            : [];

        return Results.Ok(new TodayResponse(
            elderId,
            snapshot.ServerTime,
            IsDemoData: true,
            checkIns,
            reminders));
    }

    private static async Task<IResult> CompleteReminderAsync(
        Guid reminderId,
        ReminderActionRequest request,
        HttpContext httpContext,
        ICheckInService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CompleteReminderAsync(
            reminderId,
            request.RequestId,
            httpContext.User.GetActorContext(),
            cancellationToken);
        return result.IsSuccess ? Results.Ok(ToResponse(result.Value!)) : ToProblem(result);
    }

    private static async Task<IResult> SnoozeReminderAsync(
        Guid reminderId,
        SnoozeReminderRequest request,
        HttpContext httpContext,
        ICheckInService service,
        CancellationToken cancellationToken)
    {
        var result = await service.SnoozeReminderAsync(
            reminderId,
            request.RequestId,
            request.NextReminderAt,
            httpContext.User.GetActorContext(),
            cancellationToken);
        return result.IsSuccess ? Results.Ok(ToResponse(result.Value!)) : ToProblem(result);
    }

    private static CheckInResponse ToResponse(CheckInResult result) => new(
        result.Id,
        result.RequestId,
        result.ClientTime,
        result.ReceivedAt,
        result.Kind.ToString(),
        result.IsDuplicate);

    private static ReminderActionResponse ToResponse(ReminderActionResult result) => new(
        result.ReminderId,
        result.RequestId,
        result.CompletedAt,
        result.NextDueAt,
        result.IsDuplicate);

    private static string ReminderState(Reminder reminder) => reminder.CompletedAt is not null
        ? "Completed"
        : reminder.SnoozedAt is not null
            ? "Snoozed"
            : "Pending";

    private static IResult ToProblem<T>(OperationResult<T> result)
    {
        var statusCode = result.ErrorCode switch
        {
            "FORBIDDEN_SCOPE" => StatusCodes.Status403Forbidden,
            "REASON_REQUIRED" or "INVALID_SNOOZE_TIME" or "REMINDER_COMPLETED" =>
                StatusCodes.Status400BadRequest,
            "NOT_FOUND" => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError,
        };
        return Problem(statusCode, result.ErrorCode ?? "UNKNOWN", result.ErrorMessage ?? "Request failed");
    }

    private static IResult Problem(int statusCode, string code, string title) => Results.Problem(
        statusCode: statusCode,
        title: title,
        extensions: new Dictionary<string, object?> { ["code"] = code });
}
