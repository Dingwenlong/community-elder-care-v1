namespace CommunityElderCare.Core.CareEvents;

public sealed record CareEventClosureState(
    bool HasCurrentStaffOwner,
    string? Resolution,
    bool HasIncompleteMandatoryTask,
    bool RequiresFollowUp,
    bool IsFollowUpCompleted)
{
    public static CareEventClosureState NotApplicable { get; } = new(
        HasCurrentStaffOwner: false,
        Resolution: null,
        HasIncompleteMandatoryTask: false,
        RequiresFollowUp: false,
        IsFollowUpCompleted: false);
}

public sealed record CareEventTransitionDecision(
    bool IsAllowed,
    string? ErrorCode,
    string? ErrorMessage)
{
    internal static CareEventTransitionDecision Allowed { get; } = new(true, null, null);

    internal static CareEventTransitionDecision Denied(string code, string message) =>
        new(false, code, message);
}

public static class CareEventStateMachine
{
    private static readonly IReadOnlyDictionary<CareEventStatus, IReadOnlySet<CareEventStatus>> Transitions =
        new Dictionary<CareEventStatus, IReadOnlySet<CareEventStatus>>
        {
            [CareEventStatus.PendingConfirmation] = new HashSet<CareEventStatus>
            {
                CareEventStatus.Accepted,
                CareEventStatus.FalseAlarm,
                CareEventStatus.UnableToConfirm,
            },
            [CareEventStatus.Accepted] = new HashSet<CareEventStatus>
            {
                CareEventStatus.InProgress,
                CareEventStatus.FalseAlarm,
                CareEventStatus.UnableToConfirm,
            },
            [CareEventStatus.UnableToConfirm] = new HashSet<CareEventStatus>
            {
                CareEventStatus.Accepted,
            },
            [CareEventStatus.InProgress] = new HashSet<CareEventStatus>
            {
                CareEventStatus.Resolved,
                CareEventStatus.UnableToConfirm,
            },
            [CareEventStatus.Resolved] = new HashSet<CareEventStatus>
            {
                CareEventStatus.FollowUpPending,
                CareEventStatus.Closed,
            },
            [CareEventStatus.FollowUpPending] = new HashSet<CareEventStatus>
            {
                CareEventStatus.Closed,
            },
        };

    public static bool CanTransition(CareEventStatus from, CareEventStatus to) =>
        Transitions.TryGetValue(from, out var targets) && targets.Contains(to);

    public static bool IsTerminal(CareEventStatus status) =>
        status is CareEventStatus.Closed or CareEventStatus.FalseAlarm;

    public static IReadOnlyCollection<CareEventStatus> AllowedTransitions(CareEventStatus status) =>
        Transitions.TryGetValue(status, out var targets)
            ? targets.OrderBy(target => target).ToArray()
            : [];

    public static CareEventTransitionDecision ValidateTransition(
        CareEventStatus from,
        CareEventStatus to,
        CareEventActorKind actorKind,
        string? reason,
        CareEventClosureState closureState)
    {
        if (!CanTransition(from, to))
        {
            return CareEventTransitionDecision.Denied(
                "INVALID_TRANSITION",
                $"Transition from {from} to {to} is not allowed.");
        }

        if (to == CareEventStatus.FalseAlarm && string.IsNullOrWhiteSpace(reason))
        {
            return CareEventTransitionDecision.Denied(
                "REASON_REQUIRED",
                "A false-alarm reason is required.");
        }

        if (to == CareEventStatus.FalseAlarm && actorKind != CareEventActorKind.Staff)
        {
            return CareEventTransitionDecision.Denied(
                "STAFF_CLOSE_REQUIRED",
                "Only a staff actor can mark a care event as a false alarm.");
        }

        if (to != CareEventStatus.Closed)
        {
            return CareEventTransitionDecision.Allowed;
        }

        if (actorKind != CareEventActorKind.Staff)
        {
            return CareEventTransitionDecision.Denied(
                "STAFF_CLOSE_REQUIRED",
                "Only a staff actor can close a care event.");
        }
        if (!closureState.HasCurrentStaffOwner)
        {
            return CareEventTransitionDecision.Denied(
                "OWNER_REQUIRED",
                "A current staff owner is required before closing.");
        }
        if (string.IsNullOrWhiteSpace(closureState.Resolution))
        {
            return CareEventTransitionDecision.Denied(
                "RESOLUTION_REQUIRED",
                "A resolution is required before closing.");
        }
        if (closureState.HasIncompleteMandatoryTask)
        {
            return CareEventTransitionDecision.Denied(
                "MANDATORY_TASK_INCOMPLETE",
                "All mandatory tasks must be completed before closing.");
        }
        if (closureState.RequiresFollowUp && !closureState.IsFollowUpCompleted)
        {
            return CareEventTransitionDecision.Denied(
                "FOLLOW_UP_INCOMPLETE",
                "Required follow-up must be completed before closing.");
        }

        return CareEventTransitionDecision.Allowed;
    }
}
