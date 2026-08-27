using CommunityElderCare.Api.Identity;
using CommunityElderCare.Core.Common;
using CommunityElderCare.Infrastructure.CareWork;
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
        endpoints.MapGet("/api/v1/reports/operations", OperationsAsync).RequireAuthorization();
        endpoints.MapGet("/api/v1/reports/operations.csv", ExportAsync).RequireAuthorization();
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

        var elders = dbContext.ElderProfiles.Where(e => actor.Role == DemoRole.Administrator || e.AreaCode == actor.AreaCode).Select(e => e.Id);
        var events = dbContext.CareEvents.Where(e => elders.Contains(e.ElderId)).Select(e => e.Id);
        var devices = dbContext.Devices.Where(d => elders.Contains(d.ElderId)).Select(d => d.Id);
        return Results.Ok(new
        {
            label = "当前数据",
            elderCount = await dbContext.ElderProfiles.CountAsync(item => item.IsDemoData && elders.Contains(item.Id), cancellationToken),
            openEventCount = await dbContext.CareEvents.CountAsync(item =>
                item.IsDemoData && elders.Contains(item.ElderId) &&
                item.Status != CareEventStatus.Closed &&
                item.Status != CareEventStatus.FalseAlarm,
                cancellationToken),
            completedVisitCount = await dbContext.VisitTasks.CountAsync(item =>
                item.IsDemoData && elders.Contains(item.ElderId) && item.Status == WorkStatus.Completed,
                cancellationToken),
            activeServiceOrderCount = await dbContext.ServiceOrders.CountAsync(item =>
                item.IsDemoData && elders.Contains(item.ElderId) && item.Status != WorkStatus.Completed && item.Status != WorkStatus.Cancelled,
                cancellationToken),
            simulationAttemptCount = await dbContext.NotificationAttempts.CountAsync(item =>
                item.IsDemoData && events.Contains(item.CareEventId) && item.IsSimulation,
                cancellationToken),
            deviceSignalCount = await dbContext.DeviceSignals.CountAsync(item => item.IsDemoData && devices.Contains(item.DeviceId), cancellationToken),
            confirmedMemoryCount = await dbContext.MemoryCandidates.CountAsync(item =>
                item.IsDemoData && elders.Contains(item.ElderId) && item.ConfirmedAt != null,
                cancellationToken),
        });
    }

    private static async Task<IResult> OperationsAsync(string? from, string? to, string? areaCode,
        HttpContext context, OperationsReportService service, TimeProvider clock, CancellationToken ct)
    {
        var actor = context.User.GetActorContext();
        if (!OperationsQuery.CanRead(actor) || (actor.Role == DemoRole.CommunityStaff && areaCode != null && areaCode != actor.AreaCode))
            return OperationsEndpoints.Forbidden();
        var range = OperationsDateRange.Parse(from, to, clock.GetUtcNow());
        if (range is null) return OperationsEndpoints.Problem(400, "INVALID_DATE_RANGE", "请选择不超过 90 天的有效日期。");
        return Results.Ok(await service.BuildAsync(actor, range, areaCode, ct));
    }

    private static async Task<IResult> ExportAsync(string? from, string? to, string? areaCode, string section,
        HttpContext context, OperationsReportService service, CommunityCareDbContext db, TimeProvider clock, CancellationToken ct)
    {
        var actor = context.User.GetActorContext();
        if (!OperationsQuery.CanRead(actor) || (actor.Role == DemoRole.CommunityStaff && areaCode != null && areaCode != actor.AreaCode))
            return OperationsEndpoints.Forbidden();
        var range = OperationsDateRange.Parse(from, to, clock.GetUtcNow());
        if (range is null) return OperationsEndpoints.Problem(400, "INVALID_DATE_RANGE", "请选择不超过 90 天的有效日期。");
        if (section is not "summary" and not "daily" and not "personnel")
            return OperationsEndpoints.Problem(400, "INVALID_FILTER", "请选择汇总、每日趋势或人员统计。");
        var report = await service.BuildAsync(actor, range, areaCode, ct);
        var bytes = OperationsReportService.ExportCsv(report, section);
        db.AuditEntries.Add(new AuditEntry(Guid.NewGuid(), actor.UserId, actor.Role.ToString(),
            "OperationsReportExported", "OperationsReport", Guid.NewGuid(), clock.GetUtcNow(),
            $"导出 {section}：{range.From:yyyy-MM-dd} 至 {range.To:yyyy-MM-dd}，{report.AreaLabel}", null, null));
        await db.SaveChangesAsync(ct);
        context.Response.Headers.CacheControl = "no-store";
        return Results.File(bytes, "text/csv; charset=utf-8", $"operations-{section}-{range.From:yyyyMMdd}-{range.To:yyyyMMdd}.csv");
    }
}
