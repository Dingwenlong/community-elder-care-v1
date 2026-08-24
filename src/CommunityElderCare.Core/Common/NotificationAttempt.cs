namespace CommunityElderCare.Core.Common;

public enum SimulationChannel
{
    InAppNotification,
    Sms,
    Phone,
    HomeVisit,
    EmergencyTransport,
}

public sealed class NotificationAttempt
{
    private NotificationAttempt()
    {
    }

    private NotificationAttempt(
        Guid id,
        Guid careEventId,
        Guid requestId,
        SimulationChannel channel,
        string recipientRole,
        DateTimeOffset attemptedAt,
        string outcome,
        Guid initiatedByUserId)
    {
        Id = id;
        CareEventId = careEventId;
        RequestId = requestId;
        Channel = channel;
        RecipientRole = recipientRole;
        AttemptedAt = attemptedAt;
        Outcome = outcome;
        InitiatedByUserId = initiatedByUserId;
        IsSimulation = true;
        IsDemoData = true;
    }

    public Guid Id { get; private set; }
    public Guid CareEventId { get; private set; }
    public Guid RequestId { get; private set; }
    public SimulationChannel Channel { get; private set; }
    public string RecipientRole { get; private set; } = string.Empty;
    public DateTimeOffset AttemptedAt { get; private set; }
    public string Outcome { get; private set; } = string.Empty;
    public Guid InitiatedByUserId { get; private set; }
    public bool IsSimulation { get; private set; }
    public bool IsDemoData { get; private set; } = true;

    public static NotificationAttempt Create(
        Guid id,
        Guid careEventId,
        Guid requestId,
        SimulationChannel channel,
        string recipientRole,
        DateTimeOffset attemptedAt,
        bool simulateFailure,
        Guid initiatedByUserId)
    {
        if (id == Guid.Empty || careEventId == Guid.Empty || requestId == Guid.Empty ||
            initiatedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Notification attempt identifiers are required.");
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(recipientRole);
        return new NotificationAttempt(
            id,
            careEventId,
            requestId,
            channel,
            recipientRole.Trim(),
            attemptedAt,
            simulateFailure ? "模拟失败" : "模拟送达",
            initiatedByUserId);
    }
}
