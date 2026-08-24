using CommunityElderCare.Core.Devices;

namespace CommunityElderCare.Api.Contracts.Devices;

public sealed record DeviceSignalRequest(
    Guid DeviceId,
    Guid EventId,
    DateTimeOffset DeviceTime,
    DeviceSignalType SignalType,
    string? ButtonState);

public sealed record DeviceSignalResponse(
    Guid SignalId,
    Guid CareEventId,
    DateTimeOffset ReceivedAt,
    bool IsDuplicate,
    bool IsSimulation);
