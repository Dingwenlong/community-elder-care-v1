namespace CommunityElderCare.Core.CheckIns;

public enum ReminderType
{
    Medication,
    FollowUpAppointment,
    CommunityActivity,
    VisitSchedule,
}

public sealed class Reminder
{
    private Reminder()
    {
    }

    private Reminder(
        Guid id,
        Guid elderId,
        ReminderType type,
        string demoLabel,
        DateTimeOffset dueAt)
    {
        Id = id;
        ElderId = elderId;
        Type = type;
        DemoLabel = demoLabel;
        DueAt = dueAt;
        NextDueAt = dueAt;
        IsDemoData = true;
    }

    public Guid Id { get; private set; }
    public Guid ElderId { get; private set; }
    public ReminderType Type { get; private set; }
    public string DemoLabel { get; private set; } = string.Empty;
    public DateTimeOffset DueAt { get; private set; }
    public DateTimeOffset NextDueAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public Guid? CompletedByUserId { get; private set; }
    public DateTimeOffset? SnoozedAt { get; private set; }
    public Guid? SnoozedByUserId { get; private set; }
    public bool IsDemoData { get; private set; } = true;

    public static Reminder Create(
        Guid id,
        Guid elderId,
        ReminderType type,
        string demoLabel,
        DateTimeOffset dueAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(demoLabel);
        return new Reminder(id, elderId, type, demoLabel.Trim(), dueAt);
    }

    public void Complete(DateTimeOffset completedAt, Guid actorUserId)
    {
        if (CompletedAt is not null)
        {
            return;
        }

        CompletedAt = completedAt;
        CompletedByUserId = actorUserId;
    }

    public void Snooze(DateTimeOffset now, DateTimeOffset nextDueAt, Guid actorUserId)
    {
        if (CompletedAt is not null)
        {
            throw new InvalidOperationException("A completed reminder cannot be snoozed.");
        }
        if (nextDueAt < now.AddMinutes(5) || nextDueAt > now.AddHours(24))
        {
            throw new ArgumentOutOfRangeException(nameof(nextDueAt));
        }

        SnoozedAt = now;
        SnoozedByUserId = actorUserId;
        NextDueAt = nextDueAt;
    }
}
