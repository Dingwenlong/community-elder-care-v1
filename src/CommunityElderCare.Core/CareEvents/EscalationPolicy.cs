namespace CommunityElderCare.Core.CareEvents;

public enum EscalationAction
{
    ElderReminder,
    PhoneConfirmationAttempt,
    EmergencyContactAttempt,
    CommunityNotification,
    MarkUnableToConfirm,
    Reassign,
}

public sealed record EscalationPolicy(
    TimeSpan PhoneAttemptAfter,
    TimeSpan EmergencyContactAfter,
    TimeSpan UnableToConfirmAfter)
{
    public static EscalationPolicy Demo { get; } = new(
        TimeSpan.FromMinutes(2),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(10));

    public IReadOnlyList<EscalationAction> GetDueActions(
        CareEventLevel level,
        DateTimeOffset createdAt,
        DateTimeOffset now)
    {
        if (now < createdAt)
        {
            return [];
        }
        if (level == CareEventLevel.Emergency)
        {
            return
            [
                EscalationAction.CommunityNotification,
                EscalationAction.EmergencyContactAttempt,
            ];
        }
        if (level != CareEventLevel.NeedsConfirmation)
        {
            return [];
        }

        var elapsed = now - createdAt;
        var actions = new List<EscalationAction> { EscalationAction.ElderReminder };
        if (elapsed >= PhoneAttemptAfter)
        {
            actions.Add(EscalationAction.PhoneConfirmationAttempt);
        }
        if (elapsed >= EmergencyContactAfter)
        {
            actions.Add(EscalationAction.EmergencyContactAttempt);
        }
        if (elapsed >= UnableToConfirmAfter)
        {
            actions.Add(EscalationAction.MarkUnableToConfirm);
            actions.Add(EscalationAction.Reassign);
        }
        return actions;
    }
}
