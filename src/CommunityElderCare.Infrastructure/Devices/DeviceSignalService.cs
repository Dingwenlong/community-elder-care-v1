using CommunityElderCare.Core.CareEvents;
using CommunityElderCare.Core.Common;
using CommunityElderCare.Core.Devices;
using CommunityElderCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityElderCare.Infrastructure.Devices;

public sealed class DeviceSignalService(
    CommunityCareDbContext dbContext,
    ICareEventService careEventService,
    TimeProvider timeProvider) : IDeviceSignalService
{
    public async Task<OperationResult<DeviceSignalReceipt>> ReceiveAsync(
        DeviceSignalCommand command,
        DeviceSignalIdentity identity,
        CancellationToken cancellationToken)
    {
        if (command.DeviceId == Guid.Empty || command.EventId == Guid.Empty)
        {
            return Failure("INVALID_DEVICE_SIGNAL", "Device and event IDs are required.");
        }
        if (!Enum.IsDefined(command.SignalType))
        {
            return Failure("INVALID_DEVICE_SIGNAL", "Signal type is not supported.");
        }
        if (command.ButtonState is { Length: > 32 })
        {
            return Failure("INVALID_DEVICE_SIGNAL", "Button state is too long.");
        }

        var device = await dbContext.Devices.AsNoTracking().SingleOrDefaultAsync(
            candidate => candidate.Id == command.DeviceId && candidate.IsEnabled,
            cancellationToken);
        if (device is null)
        {
            return Failure("UNKNOWN_DEVICE", "Device is not registered.");
        }
        if (identity.DeviceId != command.DeviceId)
        {
            return Failure("DEVICE_ID_MISMATCH", "Authenticated device ID does not match request.");
        }

        var duplicate = await FindSignalAsync(command.DeviceId, command.EventId, cancellationToken);
        if (duplicate is not null)
        {
            return Success(duplicate, isDuplicate: true);
        }

        var receivedAt = timeProvider.GetUtcNow();
        var mapping = Map(command.SignalType);
        var sourceEventId = $"Device:{command.DeviceId:N}:{command.EventId:N}";
        var eventResult = await careEventService.CreateAsync(
            new CreateCareEventCommand(
                device.ElderId,
                mapping.Trigger,
                CareEventSource.Device,
                sourceEventId,
                mapping.Summary,
                receivedAt,
                CareEventActorKind.Device),
            actor: null,
            cancellationToken);
        if (!eventResult.IsSuccess)
        {
            return Failure(
                eventResult.ErrorCode ?? "EVENT_CREATE_FAILED",
                eventResult.ErrorMessage ?? "Device care event could not be created.");
        }

        var signal = DeviceSignal.Receive(
            Guid.NewGuid(),
            command.DeviceId,
            command.EventId,
            eventResult.Value!.CareEvent.Id,
            command.DeviceTime,
            receivedAt,
            command.SignalType,
            command.ButtonState,
            identity.Origin == DeviceSignalOrigin.AdministratorSimulator);
        dbContext.DeviceSignals.Add(signal);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return Success(signal, isDuplicate: false);
        }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();
            var concurrent = await FindSignalAsync(
                command.DeviceId,
                command.EventId,
                cancellationToken);
            return concurrent is null
                ? Failure("PERSISTENCE_ERROR", "Device signal could not be stored.")
                : Success(concurrent, isDuplicate: true);
        }
    }

    private Task<DeviceSignal?> FindSignalAsync(
        Guid deviceId,
        Guid eventId,
        CancellationToken cancellationToken) => dbContext.DeviceSignals
        .AsNoTracking()
        .SingleOrDefaultAsync(
            signal => signal.DeviceId == deviceId && signal.EventId == eventId,
            cancellationToken);

    private static SignalMapping Map(DeviceSignalType signalType) => signalType switch
    {
        DeviceSignalType.SosButton =>
            new(CareEventTrigger.ExplicitSos, "设备 SOS 按钮触发，等待社区确认"),
        DeviceSignalType.NoWaterActivity =>
            new(CareEventTrigger.DeviceAnomaly, "长时间未检测到用水活动，等待社区确认"),
        DeviceSignalType.DeviceOffline =>
            new(CareEventTrigger.DeviceAnomaly, "照料设备离线，等待社区确认"),
        _ => throw new ArgumentOutOfRangeException(nameof(signalType), signalType, null),
    };

    private static OperationResult<DeviceSignalReceipt> Success(
        DeviceSignal signal,
        bool isDuplicate) => new(
        true,
        new DeviceSignalReceipt(
            signal.Id,
            signal.CareEventId,
            signal.ReceivedAt,
            isDuplicate,
            signal.IsSimulation),
        null,
        null);

    private static OperationResult<DeviceSignalReceipt> Failure(string code, string message) =>
        new(false, null, code, message);

    private sealed record SignalMapping(CareEventTrigger Trigger, string Summary);
}
