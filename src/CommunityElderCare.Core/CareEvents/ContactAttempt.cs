namespace CommunityElderCare.Core.CareEvents;

public enum ContactAttemptKind
{
    ElderReminder,
    PhoneConfirmation,
    EmergencyContact,
    CommunityNotification,
    Reassignment,
}

public sealed class ContactAttempt
{
    private ContactAttempt()
    {
    }

    internal ContactAttempt(
        Guid id,
        Guid careEventId,
        string deterministicAttemptId,
        ContactAttemptKind kind,
        string targetLabel,
        DateTimeOffset attemptedAt,
        string outcome)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deterministicAttemptId);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetLabel);
        ArgumentException.ThrowIfNullOrWhiteSpace(outcome);
        Id = id;
        CareEventId = careEventId;
        DeterministicAttemptId = deterministicAttemptId.Trim();
        Kind = kind;
        TargetLabel = targetLabel.Trim();
        AttemptedAt = attemptedAt;
        Outcome = outcome.Trim();
        IsSimulation = true;
    }

    public Guid Id { get; private set; }
    public Guid CareEventId { get; private set; }
    public string DeterministicAttemptId { get; private set; } = string.Empty;
    public ContactAttemptKind Kind { get; private set; }
    public string TargetLabel { get; private set; } = string.Empty;
    public DateTimeOffset AttemptedAt { get; private set; }
    public string Outcome { get; private set; } = string.Empty;
    public bool IsSimulation { get; private set; } = true;
}
