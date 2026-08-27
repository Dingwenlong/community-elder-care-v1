using CommunityElderCare.Core.Common;
using CommunityElderCare.Core.Identity;

namespace CommunityElderCare.Core.CareWork;

public sealed class FollowUp : IAssignableCareTask
{
    private FollowUp()
    {
    }

    private FollowUp(
        Guid id,
        Guid careEventId,
        Guid elderId,
        Guid assignedStaffUserId,
        DateTimeOffset dueAt,
        DateTimeOffset createdAt)
    {
        Id = id;
        CareEventId = careEventId;
        ElderId = elderId;
        AssignedStaffUserId = assignedStaffUserId;
        DueAt = dueAt;
        CreatedAt = createdAt;
        Status = WorkStatus.Assigned;
        IsMandatory = true;
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
    public DateTimeOffset DueAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? Result { get; private set; }
    public WorkStatus Status { get; private set; }
    public bool IsMandatory { get; private set; }
    public bool IsDemoData { get; private set; } = true;

    public static OperationResult<FollowUp> Create(
        Guid id,
        Guid careEventId,
        Guid elderId,
        Guid assignedStaffUserId,
        DateTimeOffset dueAt,
        DateTimeOffset createdAt)
    {
        if (id == Guid.Empty || careEventId == Guid.Empty || elderId == Guid.Empty ||
            assignedStaffUserId == Guid.Empty)
        {
            return Failure("INVALID_FOLLOW_UP", "Follow-up identifiers are required.");
        }
        if (dueAt <= createdAt)
        {
            return Failure("INVALID_DUE_TIME", "Follow-up due time must be in the future.");
        }

        return Success(new FollowUp(
            id,
            careEventId,
            elderId,
            assignedStaffUserId,
            dueAt,
            createdAt));
    }

    public OperationResult<FollowUp> Complete(
        ActorContext actor,
        string result,
        DateTimeOffset completedAt)
    {
        if (actor.Role != DemoRole.CommunityStaff || actor.UserId != AssignedStaffUserId)
        {
            return Failure("FORBIDDEN_SCOPE", "Only the assigned community staff can complete follow-up.");
        }
        if (Status != WorkStatus.Assigned)
        {
            return Failure("INVALID_WORK_STATUS", "Only an assigned follow-up can complete.");
        }
        if (string.IsNullOrWhiteSpace(result))
        {
            return Failure("RESULT_REQUIRED", "A follow-up result is required.");
        }

        Result = result.Trim();
        CompletedAt = completedAt;
        Status = WorkStatus.Completed;
        return Success(this);
    }

    private static OperationResult<FollowUp> Success(FollowUp followUp) =>
        new(true, followUp, null, null);

    private static OperationResult<FollowUp> Failure(string code, string message) =>
        new(false, null, code, message);
}
