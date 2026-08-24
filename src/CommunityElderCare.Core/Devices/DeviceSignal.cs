namespace CommunityElderCare.Core.Devices;

public sealed class DeviceSignal
{
    private DeviceSignal()
    {
    }

    private DeviceSignal(
        Guid id,
        Guid deviceId,
        Guid eventId,
        Guid careEventId,
        DateTimeOffset deviceTime,
        DateTimeOffset receivedAt,
        DeviceSignalType signalType,
        string? buttonState,
        bool isSimulation)
    {
        Id = id;
        DeviceId = deviceId;
        EventId = eventId;
        CareEventId = careEventId;
        DeviceTime = deviceTime;
        ReceivedAt = receivedAt;
        SignalType = signalType;
        ButtonState = buttonState;
        IsSimulation = isSimulation;
        IsDemoData = true;
    }

    public Guid Id { get; private set; }
    public Guid DeviceId { get; private set; }
    public Guid EventId { get; private set; }
    public Guid CareEventId { get; private set; }
    public DateTimeOffset DeviceTime { get; private set; }
    public DateTimeOffset ReceivedAt { get; private set; }
    public DeviceSignalType SignalType { get; private set; }
    public string? ButtonState { get; private set; }
    public bool IsSimulation { get; private set; }
    public bool IsDemoData { get; private set; } = true;

    public static DeviceSignal Receive(
        Guid id,
        Guid deviceId,
        Guid eventId,
        Guid careEventId,
        DateTimeOffset deviceTime,
        DateTimeOffset receivedAt,
        DeviceSignalType signalType,
        string? buttonState,
        bool isSimulation) => new(
        id,
        deviceId,
        eventId,
        careEventId,
        deviceTime,
        receivedAt,
        signalType,
        buttonState?.Trim(),
        isSimulation);
}
