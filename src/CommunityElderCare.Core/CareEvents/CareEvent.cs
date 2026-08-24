namespace CommunityElderCare.Core.CareEvents;

public sealed class CareEvent
{
    private readonly List<CareEventEvidence> _evidence = [];
    private readonly List<CareEventTransition> _transitions = [];
    private readonly List<ContactAttempt> _contactAttempts = [];

    private CareEvent()
    {
    }

    private CareEvent(
        Guid id,
        Guid elderId,
        CareEventCategory category,
        CareEventLevel level,
        CareEventSource source,
        string sourceEventId,
        string summary,
        DateTimeOffset occurredAt,
        string responsibilityQueue,
        DateTimeOffset? createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceEventId);
        ArgumentException.ThrowIfNullOrWhiteSpace(summary);
        ArgumentException.ThrowIfNullOrWhiteSpace(responsibilityQueue);
        Id = id;
        ElderId = elderId;
        Category = category;
        Level = level;
        Status = CareEventStatus.PendingConfirmation;
        Source = source;
        SourceEventId = sourceEventId.Trim();
        Summary = summary.Trim();
        OccurredAt = occurredAt;
        CreatedAt = createdAt ?? occurredAt;
        LastActivityAt = CreatedAt;
        ResponsibilityQueue = responsibilityQueue.Trim();
        IsDemoData = true;
    }

    public Guid Id { get; private set; }
    public Guid ElderId { get; private set; }
    public CareEventCategory Category { get; private set; }
    public CareEventLevel Level { get; private set; }
    public CareEventStatus Status { get; private set; }
    public CareEventSource Source { get; private set; }
    public string SourceEventId { get; private set; } = string.Empty;
    public string Summary { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset LastActivityAt { get; private set; }
    public string ResponsibilityQueue { get; private set; } = string.Empty;
    public Guid? CurrentOwnerUserId { get; private set; }
    public string? Resolution { get; private set; }
    public bool RequiresFollowUp { get; private set; }
    public bool IsFollowUpCompleted { get; private set; }
    public bool HasIncompleteMandatoryTask { get; private set; }
    public bool IsDemoData { get; private set; } = true;
    public IReadOnlyCollection<CareEventEvidence> Evidence => _evidence.AsReadOnly();
    public IReadOnlyCollection<CareEventTransition> Transitions => _transitions.AsReadOnly();
    public IReadOnlyCollection<ContactAttempt> ContactAttempts => _contactAttempts.AsReadOnly();

    public static CareEvent Create(
        Guid id,
        Guid elderId,
        CareEventCategory category,
        CareEventLevel level,
        CareEventSource source,
        string sourceEventId,
        string summary,
        DateTimeOffset occurredAt,
        string responsibilityQueue,
        DateTimeOffset? createdAt = null) =>
        new(
            id,
            elderId,
            category,
            level,
            source,
            sourceEventId,
            summary,
            occurredAt,
            responsibilityQueue,
            createdAt);

    public CareEventTransitionDecision Accept(Guid staffUserId, DateTimeOffset occurredAt)
    {
        if (staffUserId == Guid.Empty)
        {
            return CareEventTransitionDecision.Denied("OWNER_REQUIRED", "A staff owner is required.");
        }
        if (!CareEventStateMachine.CanTransition(Status, CareEventStatus.Accepted))
        {
            return CareEventTransitionDecision.Denied(
                "INVALID_TRANSITION",
                $"Transition from {Status} to Accepted is not allowed.");
        }

        var from = Status;
        CurrentOwnerUserId = staffUserId;
        Status = CareEventStatus.Accepted;
        LastActivityAt = occurredAt;
        _transitions.Add(new CareEventTransition(
            Guid.NewGuid(),
            Id,
            from,
            Status,
            CareEventActorKind.Staff,
            staffUserId,
            "工作人员接单",
            occurredAt));
        return CareEventTransitionDecision.Allowed;
    }

    public CareEventTransitionDecision TryTransition(
        CareEventStatus target,
        CareEventActorKind actorKind,
        Guid? actorUserId,
        string? reason,
        string? resolution,
        DateTimeOffset occurredAt)
    {
        var closureState = new CareEventClosureState(
            CurrentOwnerUserId is not null,
            string.IsNullOrWhiteSpace(resolution) ? Resolution : resolution,
            HasIncompleteMandatoryTask,
            RequiresFollowUp,
            IsFollowUpCompleted);
        var decision = CareEventStateMachine.ValidateTransition(
            Status,
            target,
            actorKind,
            reason,
            closureState);
        if (!decision.IsAllowed)
        {
            return decision;
        }

        var from = Status;
        Status = target;
        if (!string.IsNullOrWhiteSpace(resolution))
        {
            Resolution = resolution.Trim();
        }
        if (target == CareEventStatus.FalseAlarm)
        {
            Resolution = reason!.Trim();
        }
        LastActivityAt = occurredAt;
        _transitions.Add(new CareEventTransition(
            Guid.NewGuid(),
            Id,
            from,
            target,
            actorKind,
            actorUserId,
            reason,
            occurredAt));
        return decision;
    }

    public void AddEvidence(
        Guid evidenceId,
        string kind,
        string summary,
        DateTimeOffset occurredAt,
        DateTimeOffset recordedAt,
        string? sourceEventId)
    {
        _evidence.Add(new CareEventEvidence(
            evidenceId,
            Id,
            kind,
            summary,
            occurredAt,
            recordedAt,
            sourceEventId));
        if (recordedAt > LastActivityAt)
        {
            LastActivityAt = recordedAt;
        }
    }

    public bool AddContactAttempt(
        Guid attemptId,
        string deterministicAttemptId,
        ContactAttemptKind kind,
        string targetLabel,
        DateTimeOffset attemptedAt,
        string outcome)
    {
        if (_contactAttempts.Any(attempt =>
                attempt.DeterministicAttemptId == deterministicAttemptId))
        {
            return false;
        }

        _contactAttempts.Add(new ContactAttempt(
            attemptId,
            Id,
            deterministicAttemptId,
            kind,
            targetLabel,
            attemptedAt,
            outcome));
        LastActivityAt = attemptedAt;
        return true;
    }

    public void SetWorkState(
        bool hasIncompleteMandatoryTask,
        bool requiresFollowUp,
        bool isFollowUpCompleted)
    {
        HasIncompleteMandatoryTask = hasIncompleteMandatoryTask;
        RequiresFollowUp = requiresFollowUp;
        IsFollowUpCompleted = isFollowUpCompleted;
    }

    public void ClearOwnerForReassignment()
    {
        CurrentOwnerUserId = null;
    }
}
