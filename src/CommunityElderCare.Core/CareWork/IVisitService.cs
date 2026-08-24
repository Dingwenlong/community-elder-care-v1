using CommunityElderCare.Core.Common;
using CommunityElderCare.Core.Identity;

namespace CommunityElderCare.Core.CareWork;

public sealed record CreateVisitCommand(
    Guid CareEventId,
    Guid AssignedStaffUserId,
    DateTimeOffset ScheduledStartAt,
    DateTimeOffset ScheduledEndAt,
    bool IsMandatory);

public sealed record CompleteVisitCommand(
    string RawStaffNote,
    string ConfirmedSummary,
    string Result);

public sealed record CreateFollowUpCommand(
    Guid CareEventId,
    Guid AssignedStaffUserId,
    DateTimeOffset DueAt);

public interface IVisitService
{
    Task<OperationResult<VisitTask>> CreateAsync(
        CreateVisitCommand command,
        ActorContext actor,
        CancellationToken cancellationToken);

    Task<OperationResult<VisitTask>> StartAsync(
        Guid visitId,
        ActorContext actor,
        CancellationToken cancellationToken);

    Task<OperationResult<VisitTask>> CompleteAsync(
        Guid visitId,
        CompleteVisitCommand command,
        ActorContext actor,
        CancellationToken cancellationToken);

    Task<OperationResult<FollowUp>> CreateFollowUpAsync(
        CreateFollowUpCommand command,
        ActorContext actor,
        CancellationToken cancellationToken);

    Task<OperationResult<FollowUp>> CompleteFollowUpAsync(
        Guid followUpId,
        string result,
        ActorContext actor,
        CancellationToken cancellationToken);
}
