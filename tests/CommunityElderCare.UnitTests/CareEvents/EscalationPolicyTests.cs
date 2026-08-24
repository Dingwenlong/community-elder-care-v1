using CommunityElderCare.Core.CareEvents;

namespace CommunityElderCare.UnitTests.CareEvents;

public sealed class EscalationPolicyTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 8, 24, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Needs_confirmation_uses_the_demo_escalation_timeline()
    {
        var policy = EscalationPolicy.Demo;

        Assert.Equal(
            [EscalationAction.ElderReminder],
            policy.GetDueActions(CareEventLevel.NeedsConfirmation, CreatedAt, CreatedAt));
        Assert.Contains(
            EscalationAction.PhoneConfirmationAttempt,
            policy.GetDueActions(CareEventLevel.NeedsConfirmation, CreatedAt, CreatedAt.AddMinutes(2)));
        Assert.Contains(
            EscalationAction.EmergencyContactAttempt,
            policy.GetDueActions(CareEventLevel.NeedsConfirmation, CreatedAt, CreatedAt.AddMinutes(5)));
        var overdue = policy.GetDueActions(
            CareEventLevel.NeedsConfirmation,
            CreatedAt,
            CreatedAt.AddMinutes(10));
        Assert.Contains(EscalationAction.MarkUnableToConfirm, overdue);
        Assert.Contains(EscalationAction.Reassign, overdue);
    }

    [Fact]
    public void Emergency_notifies_community_and_contact_immediately()
    {
        var actions = EscalationPolicy.Demo.GetDueActions(
            CareEventLevel.Emergency,
            CreatedAt,
            CreatedAt);

        Assert.Contains(EscalationAction.CommunityNotification, actions);
        Assert.Contains(EscalationAction.EmergencyContactAttempt, actions);
    }
}
