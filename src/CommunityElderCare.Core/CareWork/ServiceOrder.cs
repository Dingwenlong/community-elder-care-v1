using CommunityElderCare.Core.Common;
using CommunityElderCare.Core.Identity;

namespace CommunityElderCare.Core.CareWork;

public sealed class ServiceOrder : IAssignableCareTask
{
    private ServiceOrder()
    {
    }

    private ServiceOrder(
        Guid id,
        Guid careEventId,
        Guid elderId,
        string serviceType,
        string scheduledWindow,
        string contactInstruction,
        Guid assignedWorkerUserId,
        bool isMandatory,
        DateTimeOffset createdAt)
    {
        Id = id;
        CareEventId = careEventId;
        ElderId = elderId;
        ServiceType = serviceType;
        ScheduledWindow = scheduledWindow;
        ContactInstruction = contactInstruction;
        AssignedWorkerUserId = assignedWorkerUserId;
        IsMandatory = isMandatory;
        CreatedAt = createdAt;
        Status = WorkStatus.Assigned;
        IsDemoData = true;
    }

    public Guid Version { get; private set; } = Guid.NewGuid();
    Guid IAssignableCareTask.AssignedUserId => AssignedWorkerUserId;

    public void Reassign(Guid userId)
    {
        if (Status != WorkStatus.Assigned || userId == Guid.Empty)
            throw new InvalidOperationException("Only unstarted tasks can be reassigned.");
        AssignedWorkerUserId = userId;
    }

    public Guid Id { get; private set; }
    public Guid CareEventId { get; private set; }
    public Guid ElderId { get; private set; }
    public string ServiceType { get; private set; } = string.Empty;
    public string ScheduledWindow { get; private set; } = string.Empty;
    public string ContactInstruction { get; private set; } = string.Empty;
    public Guid AssignedWorkerUserId { get; private set; }
    public bool IsMandatory { get; private set; }
    public WorkStatus Status { get; private set; }
    public DateTimeOffset? DueAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? AcceptedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public string? Result { get; private set; }
    public string? CancellationReason { get; private set; }
    public bool IsDemoData { get; private set; } = true;

    public static OperationResult<ServiceOrder> Create(
        Guid id,
        Guid careEventId,
        Guid elderId,
        string serviceType,
        string scheduledWindow,
        string contactInstruction,
        Guid assignedWorkerUserId,
        bool isMandatory,
        DateTimeOffset createdAt,
        DateTimeOffset? dueAt = null)
    {
        if (id == Guid.Empty || careEventId == Guid.Empty || elderId == Guid.Empty ||
            assignedWorkerUserId == Guid.Empty ||
            string.IsNullOrWhiteSpace(serviceType) ||
            string.IsNullOrWhiteSpace(scheduledWindow) ||
            string.IsNullOrWhiteSpace(contactInstruction))
        {
            return Failure("INVALID_SERVICE_ORDER", "Service order fields are required.");
        }

        return Success(new ServiceOrder(
            id,
            careEventId,
            elderId,
            serviceType.Trim(),
            scheduledWindow.Trim(),
            contactInstruction.Trim(),
            assignedWorkerUserId,
            isMandatory,
            createdAt) { DueAt = dueAt });
    }

    public OperationResult<ServiceOrder> Accept(ActorContext actor, DateTimeOffset acceptedAt)
    {
        if (!IsAssignedWorker(actor))
        {
            return Failure("FORBIDDEN_SCOPE", "Only the assigned service worker can accept the order.");
        }
        if (Status != WorkStatus.Assigned)
        {
            return Failure("INVALID_WORK_STATUS", "Only an assigned order can be accepted.");
        }

        AcceptedAt = acceptedAt;
        Status = WorkStatus.InProgress;
        return Success(this);
    }

    public OperationResult<ServiceOrder> Complete(
        ActorContext actor,
        string result,
        DateTimeOffset completedAt)
    {
        if (!IsAssignedWorker(actor))
        {
            return Failure("FORBIDDEN_SCOPE", "Only the assigned service worker can complete the order.");
        }
        if (Status is not WorkStatus.Assigned and not WorkStatus.InProgress)
        {
            return Failure("INVALID_WORK_STATUS", "Only an assigned or in-progress order can complete.");
        }
        if (string.IsNullOrWhiteSpace(result))
        {
            return Failure("RESULT_REQUIRED", "A service result is required.");
        }

        Result = result.Trim();
        CompletedAt = completedAt;
        Status = WorkStatus.Completed;
        return Success(this);
    }

    public OperationResult<ServiceOrder> Cancel(
        ActorContext actor,
        string reason,
        DateTimeOffset cancelledAt)
    {
        if (actor.Role != DemoRole.CommunityStaff)
        {
            return Failure("FORBIDDEN_SCOPE", "Only community staff can cancel a service order.");
        }
        if (string.IsNullOrWhiteSpace(reason))
        {
            return Failure("REASON_REQUIRED", "A cancellation reason is required.");
        }
        if (Status is WorkStatus.Completed or WorkStatus.Cancelled)
        {
            return Failure("INVALID_WORK_STATUS", "Completed or cancelled order cannot be cancelled.");
        }

        CancellationReason = reason.Trim();
        CancelledAt = cancelledAt;
        Status = WorkStatus.Cancelled;
        return Success(this);
    }

    private bool IsAssignedWorker(ActorContext actor) =>
        actor.Role == DemoRole.ServiceWorker &&
        actor.UserId == AssignedWorkerUserId;

    private static OperationResult<ServiceOrder> Success(ServiceOrder order) =>
        new(true, order, null, null);

    private static OperationResult<ServiceOrder> Failure(string code, string message) =>
        new(false, null, code, message);
}
