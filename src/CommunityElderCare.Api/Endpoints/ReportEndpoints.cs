using CommunityElderCare.Api.Identity;
using CommunityElderCare.Core.CareEvents;
using CommunityElderCare.Core.CareWork;
using CommunityElderCare.Core.Identity;
using CommunityElderCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityElderCare.Api.Endpoints;

public static class ReportEndpoints
{
    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/reports/demo-summary", SummaryAsync).RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> SummaryAsync(
        HttpContext httpContext,
        CommunityCareDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var actor = httpContext.User.GetActorContext();
        if (actor.Role is not DemoRole.Administrator and not DemoRole.CommunityStaff)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Report scope is required",
                extensions: new Dictionary<string, object?> { ["code"] = "FORBIDDEN_SCOPE" });
        }

        return Results.Ok(new
        {
            label = "运行数据",
            elderCount = await dbContext.ElderProfiles.CountAsync(item => item.IsDemoData, cancellationToken),
            openEventCount = await dbContext.CareEvents.CountAsync(item =>
                item.IsDemoData &&
                item.Status != CareEventStatus.Closed &&
                item.Status != CareEventStatus.FalseAlarm,
                cancellationToken),
            completedVisitCount = await dbContext.VisitTasks.CountAsync(item =>
                item.IsDemoData && item.Status == WorkStatus.Completed,
                cancellationToken),
            activeServiceOrderCount = await dbContext.ServiceOrders.CountAsync(item =>
                item.IsDemoData && item.Status != WorkStatus.Completed && item.Status != WorkStatus.Cancelled,
                cancellationToken),
            simulationAttemptCount = await dbContext.NotificationAttempts.CountAsync(item =>
                item.IsDemoData && item.IsSimulation,
                cancellationToken),
            deviceSignalCount = await dbContext.DeviceSignals.CountAsync(item => item.IsDemoData, cancellationToken),
            confirmedMemoryCount = await dbContext.MemoryCandidates.CountAsync(item =>
                item.IsDemoData && item.ConfirmedAt != null,
                cancellationToken),
        });
    }
}
