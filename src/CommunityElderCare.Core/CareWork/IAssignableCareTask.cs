namespace CommunityElderCare.Core.CareWork;

public interface IAssignableCareTask
{
    Guid Id { get; }
    Guid CareEventId { get; }
    Guid ElderId { get; }
    Guid AssignedUserId { get; }
    Guid Version { get; }
    WorkStatus Status { get; }
    void Reassign(Guid userId);
}

public sealed class TaskReassignment
{
    public Guid Id { get; set; }
    public string TaskType { get; set; } = string.Empty;
    public Guid TaskId { get; set; }
    public Guid CareEventId { get; set; }
    public Guid FromUserId { get; set; }
    public Guid ToUserId { get; set; }
    public Guid ActorUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
    public bool IsDemoData { get; set; } = true;
}
