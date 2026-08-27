using CommunityElderCare.Core.Common;
using CommunityElderCare.Core.Identity;

namespace CommunityElderCare.Core.CareWork;

public sealed class VisitTask : IAssignableCareTask
{
    private VisitTask()
    {
    }

    private VisitTask(
        Guid id,
        Guid careEventId,
        Guid elderId,
        Guid assignedStaffUserId,
        DateTimeOffset scheduledStartAt,
        DateTimeOffset scheduledEndAt,
        bool isMandatory,
        DateTimeOffset createdAt)
    {
        Id = id;
        CareEventId = careEventId;
        ElderId = elderId;
        AssignedStaffUserId = assignedStaffUserId;
        ScheduledStartAt = scheduledStartAt;
        ScheduledEndAt = scheduledEndAt;
        IsMandatory = isMandatory;
        CreatedAt = createdAt;
        Status = WorkStatus.Assigned;
        IsDemoData = true;
    }

    public Guid Version { get; private set; } = Guid.NewGuid();
    Guid IAssignableCareTask.AssignedUserId => AssignedStaffUserId;

    public void Reassign(Guid userId)
    {
        if (Status != WorkStatus.Assigned || userId == Guid.Empty)
            throw new InvalidOperationException("Only unstarted tasks can be reassigned.");
        AssignedStaffUserId = userId;
    }

    public Guid Id { get; private set; }
    public Guid CareEventId { get; private set; }
    public Guid ElderId { get; private set; }
    public Guid AssignedStaffUserId { get; private set; }
    public DateTimeOffset ScheduledStartAt { get; private set; }
    public DateTimeOffset ScheduledEndAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public string? RawStaffNote { get; private set; }
    public string? ConfirmedSummary { get; private set; }
    public string? Result { get; private set; }
    public string? CancellationReason { get; private set; }
    public WorkStatus Status { get; private set; }
    public bool IsMandatory { get; private set; }
    public bool IsDemoData { get; private set; } = true;

    public static OperationResult<VisitTask> Create(
        Guid id,
        Guid careEventId,
        Guid elderId,
        Guid assignedStaffUserId,
        DateTimeOffset scheduledStartAt,
        DateTimeOffset scheduledEndAt,
        bool isMandatory,
        DateTimeOffset createdAt)
    {
        if (id == Guid.Empty || careEventId == Guid.Empty || elderId == Guid.Empty ||
            assignedStaffUserId == Guid.Empty)
        {
            return Failure("INVALID_VISIT", "Visit identifiers are required.");
        }
        if (scheduledEndAt <= scheduledStartAt)
        {
            return Failure("INVALID_VISIT_WINDOW", "Visit end must be after its start.");
        }

        return Success(new VisitTask(
            id,
            careEventId,
            elderId,
            assignedStaffUserId,
            scheduledStartAt,
            scheduledEndAt,
            isMandatory,
            createdAt));
    }

    public OperationResult<VisitTask> Start(ActorContext actor, DateTimeOffset startedAt)
    {
        if (!IsAssignedStaff(actor))
        {
            return Failure("FORBIDDEN_SCOPE", "Only the assigned community staff can start the visit.");
        }
        if (Status != WorkStatus.Assigned)
        {
            return Failure("INVALID_WORK_STATUS", "Only an assigned visit can start.");
        }

        Status = WorkStatus.InProgress;
        StartedAt = startedAt;
        return Success(this);
    }

    public OperationResult<VisitTask> Complete(
        ActorContext actor,
        string rawStaffNote,
        string confirmedSummary,
        string result,
        DateTimeOffset completedAt)
    {
        if (!IsAssignedStaff(actor))
        {
            return Failure("FORBIDDEN_SCOPE", "Only the assigned community staff can complete the visit.");
        }
        if (Status != WorkStatus.InProgress)
        {
            return Failure("INVALID_WORK_STATUS", "Visit must start before completion.");
        }
        if (string.IsNullOrWhiteSpace(rawStaffNote) ||
            string.IsNullOrWhiteSpace(confirmedSummary) ||
            string.IsNullOrWhiteSpace(result))
        {
            return Failure("RESULT_REQUIRED", "Visit notes, confirmed summary and result are required.");
        }

        RawStaffNote = rawStaffNote.Trim();
        ConfirmedSummary = confirmedSummary.Trim();
        Result = result.Trim();
        CompletedAt = completedAt;
        Status = WorkStatus.Completed;
        return Success(this);
    }

    public OperationResult<VisitTask> Cancel(
        ActorContext actor,
        string reason,
        DateTimeOffset cancelledAt)
    {
        if (!IsAssignedStaff(actor))
        {
            return Failure("FORBIDDEN_SCOPE", "Only the assigned community staff can cancel the visit.");
        }
        if (string.IsNullOrWhiteSpace(reason))
        {
            return Failure("REASON_REQUIRED", "A cancellation reason is required.");
        }
        if (Status is WorkStatus.Completed or WorkStatus.Cancelled)
        {
            return Failure("INVALID_WORK_STATUS", "Completed or cancelled visit cannot be cancelled.");
        }

        CancellationReason = reason.Trim();
        CancelledAt = cancelledAt;
        Status = WorkStatus.Cancelled;
        return Success(this);
    }

    private bool IsAssignedStaff(ActorContext actor) =>
        actor.Role == DemoRole.CommunityStaff && actor.UserId == AssignedStaffUserId;

    private static OperationResult<VisitTask> Success(VisitTask visit) =>
        new(true, visit, null, null);

    private static OperationResult<VisitTask> Failure(string code, string message) =>
        new(false, null, code, message);
}
