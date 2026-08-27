using CommunityElderCare.Api.Identity;
using CommunityElderCare.Core.Common;
using CommunityElderCare.Core.Devices;
using CommunityElderCare.Core.Identity;
using CommunityElderCare.Infrastructure.CareWork;
using CommunityElderCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityElderCare.Api.Endpoints;

public sealed record SetDeviceEnabledRequest(bool Enabled, string Reason, Guid ExpectedVersion);

public static class DeviceManagementEndpoints
{
    public static IEndpointRouteBuilder MapDeviceManagementEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/devices", ListAsync).RequireAuthorization();
        endpoints.MapGet("/api/v1/devices/{id:guid}/signals", SignalsAsync).RequireAuthorization();
        endpoints.MapPatch("/api/v1/devices/{id:guid}/enabled", SetEnabledAsync).RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> ListAsync(HttpContext context, CommunityCareDbContext db, CancellationToken ct)
    {
        if (context.User.GetActorContext().Role != DemoRole.Administrator) return OperationsEndpoints.Forbidden();
        var devices = await (from d in db.Devices.AsNoTracking()
            join e in db.ElderProfiles.AsNoTracking() on d.ElderId equals e.Id
            select new { deviceId = d.Id, d.DisplayName, elderDisplayName = e.DemoDisplayName, e.AreaCode,
                d.IsEnabled, d.RegisteredAt, version = EF.Property<Guid>(d, "Version") }).ToListAsync(ct);
        var signals = await db.DeviceSignals.AsNoTracking()
            .Select(s => new { s.DeviceId, s.ReceivedAt, s.IsSimulation }).ToListAsync(ct);
        return Results.Ok(devices.Select(d => new
        {
            d.deviceId, d.DisplayName, d.elderDisplayName, d.AreaCode, d.IsEnabled, d.RegisteredAt, d.version,
            lastHardwareSignalAt = signals.Where(s => s.DeviceId == d.deviceId && !s.IsSimulation)
                .Select(s => (DateTimeOffset?)s.ReceivedAt).Max(),
            lastSimulationSignalAt = signals.Where(s => s.DeviceId == d.deviceId && s.IsSimulation)
                .Select(s => (DateTimeOffset?)s.ReceivedAt).Max(),
        }));
    }

    private static async Task<IResult> SignalsAsync(Guid id, string? from, string? to,
        DeviceSignalType? signalType, bool? isSimulation, HttpContext context,
        CommunityCareDbContext db, TimeProvider clock, CancellationToken ct)
    {
        if (context.User.GetActorContext().Role != DemoRole.Administrator) return OperationsEndpoints.Forbidden();
        var range = OperationsDateRange.Parse(from, to, clock.GetUtcNow());
        if (range is null) return OperationsEndpoints.Problem(400, "INVALID_DATE_RANGE", "请选择不超过 90 天的有效日期。");
        if (signalType.HasValue && !Enum.IsDefined(signalType.Value))
            return OperationsEndpoints.Problem(400, "INVALID_FILTER", "信号类型不正确。");
        if (!await db.Devices.AnyAsync(d => d.Id == id, ct)) return OperationsEndpoints.Problem(404, "NOT_FOUND", "设备不存在。");
        var signals = await (from s in db.DeviceSignals.AsNoTracking()
            join e in db.CareEvents.AsNoTracking() on s.CareEventId equals e.Id
            where s.DeviceId == id && (!signalType.HasValue || s.SignalType == signalType) &&
                (!isSimulation.HasValue || s.IsSimulation == isSimulation)
            select new { signalId = s.Id, s.EventId, s.CareEventId, s.SignalType, s.DeviceTime,
                s.ReceivedAt, s.IsSimulation, careEventStatus = e.Status }).ToListAsync(ct);
        return Results.Ok(signals.Where(s => range.Contains(s.ReceivedAt)).OrderByDescending(s => s.ReceivedAt));
    }

    private static async Task<IResult> SetEnabledAsync(Guid id, SetDeviceEnabledRequest request,
        HttpContext context, CommunityCareDbContext db, TimeProvider clock, CancellationToken ct)
    {
        var actor = context.User.GetActorContext();
        if (actor.Role != DemoRole.Administrator) return OperationsEndpoints.Forbidden();
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 512)
            return OperationsEndpoints.Problem(400, "REASON_REQUIRED", "请填写不超过 512 字的启停原因。");
        var device = await db.Devices.SingleOrDefaultAsync(d => d.Id == id, ct);
        if (device is null) return OperationsEndpoints.Problem(404, "NOT_FOUND", "设备不存在。");
        if (db.Entry(device).Property<Guid>("Version").CurrentValue != request.ExpectedVersion)
            return OperationsEndpoints.Problem(409, "CONCURRENT_CHANGE", "设备已更新，请刷新后重试。");
        if (device.IsEnabled == request.Enabled) return Results.Ok(new { deviceId = id, device.IsEnabled });
        var before = device.IsEnabled ? "Enabled" : "Disabled";
        device.SetEnabled(request.Enabled);
        db.AuditEntries.Add(new AuditEntry(Guid.NewGuid(), actor.UserId, actor.Role.ToString(),
            "DeviceEnabledChanged", "Device", id, clock.GetUtcNow(), request.Reason.Trim(),
            before, request.Enabled ? "Enabled" : "Disabled"));
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { deviceId = id, device.IsEnabled });
    }
}
