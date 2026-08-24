namespace CommunityElderCare.Core.Identity;

public sealed record ActorContext(
    Guid UserId,
    DemoRole Role,
    Guid? ElderId,
    string? AreaCode,
    Guid? AssignedTaskId);
