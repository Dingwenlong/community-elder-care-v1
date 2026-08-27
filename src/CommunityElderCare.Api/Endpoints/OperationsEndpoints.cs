using CommunityElderCare.Api.Identity;
using CommunityElderCare.Core.CareWork;
using CommunityElderCare.Infrastructure.CareWork;
using CommunityElderCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityElderCare.Api.Endpoints;

public sealed record ReassignTaskRequest(Guid AssignedUserId, string Reason, Guid ExpectedVersion);

public static class OperationsEndpoints
{
    public static IEndpointRouteBuilder MapOperationsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/operations/personnel", async (HttpContext context, OperationsQuery query, CancellationToken ct) =>
        {
            var actor = context.User.GetActorContext();
            return OperationsQuery.CanRead(actor) ? Results.Ok(await query.PersonnelAsync(actor, ct)) : Forbidden();
        }).RequireAuthorization();
        endpoints.MapGet("/api/v1/operations/tasks", async (HttpContext context, OperationsQuery query,
            string? taskType, Guid? assignedUserId, WorkStatus? status, bool? overdueOnly, CancellationToken ct) =>
        {
            var actor = context.User.GetActorContext();
            if (!OperationsQuery.CanRead(actor)) return Forbidden();
            if (taskType is not null and not "Visit" and not "ServiceOrder" and not "FollowUp")
                return Problem(400, "INVALID_FILTER", "任务类型不正确。");
            var tasks = await query.TasksAsync(actor, ct);
            return Results.Ok(tasks.Where(t => (taskType == null || t.TaskType == taskType) &&
                (assignedUserId == null || t.AssignedUserId == assignedUserId) &&
                (status == null || t.Status == status) && (overdueOnly != true || t.IsOverdue)));
        }).RequireAuthorization();
        foreach (var (route, type) in new[] { ("visits", "Visit"), ("service-orders", "ServiceOrder"), ("follow-ups", "FollowUp") })
        {
            endpoints.MapPost($"/api/v1/{route}/{{id:guid}}/reassign",
                async (Guid id, ReassignTaskRequest request, HttpContext context, TaskAssignmentService service, CancellationToken ct) =>
                {
                    var result = await service.ReassignAsync(type, id, request.AssignedUserId,
                        request.Reason, request.ExpectedVersion, context.User.GetActorContext(), ct);
                    return result.IsSuccess ? Results.Ok(new { taskId = id }) : Problem(
                        result.ErrorCode switch { "NOT_FOUND" => 404, "FORBIDDEN_SCOPE" => 403,
                            "CONCURRENT_CHANGE" or "INVALID_WORK_STATUS" => 409, _ => 400 },
                        result.ErrorCode!, result.ErrorMessage!);
                }).RequireAuthorization();
        }
        endpoints.MapGet("/api/v1/operations/tasks/{taskId:guid}/reassignments",
            async (Guid taskId, HttpContext context, OperationsQuery query, CommunityCareDbContext db, CancellationToken ct) =>
            {
                var actor = context.User.GetActorContext();
                if (!OperationsQuery.CanRead(actor)) return Forbidden();
                if (!(await query.TasksAsync(actor, ct)).Any(t => t.TaskId == taskId))
                    return Problem(404, "NOT_FOUND", "任务不存在。");
                var records = await db.TaskReassignments.AsNoTracking().Where(r => r.TaskId == taskId).ToListAsync(ct);
                return Results.Ok(records.OrderBy(r => r.OccurredAt));
            }).RequireAuthorization();
        return endpoints;
    }

    internal static IResult Forbidden() => Problem(403, "FORBIDDEN_SCOPE", "当前账号没有操作权限。");
    internal static IResult Problem(int status, string code, string title) => Results.Problem(
        statusCode: status, title: title, extensions: new Dictionary<string, object?> { ["code"] = code });
}
