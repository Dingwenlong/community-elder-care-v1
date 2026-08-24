using CommunityElderCare.Core.Common;

namespace CommunityElderCare.Core.Devices;

public enum DeviceSignalOrigin
{
    Hardware,
    AdministratorSimulator,
}

public sealed record DeviceSignalIdentity(
    Guid DeviceId,
    DeviceSignalOrigin Origin,
    Guid? UserId);

public sealed record DeviceSignalCommand(
    Guid DeviceId,
    Guid EventId,
    DateTimeOffset DeviceTime,
    DeviceSignalType SignalType,
    string? ButtonState);

public sealed record DeviceSignalReceipt(
    Guid SignalId,
    Guid CareEventId,
    DateTimeOffset ReceivedAt,
    bool IsDuplicate,
    bool IsSimulation);

public interface IDeviceSignalService
{
    Task<OperationResult<DeviceSignalReceipt>> ReceiveAsync(
        DeviceSignalCommand command,
        DeviceSignalIdentity identity,
        CancellationToken cancellationToken);
}
