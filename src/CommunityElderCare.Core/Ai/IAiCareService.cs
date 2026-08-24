using CommunityElderCare.Core.Common;
using CommunityElderCare.Core.Identity;

namespace CommunityElderCare.Core.Ai;

public sealed record AiChatCommand(
    Guid ElderId,
    string SessionId,
    string Input);

public sealed record AiChatResult(
    string Reply,
    bool UsedFallback,
    DangerCueResult DangerCue,
    Guid? CareEventId,
    string? RejectionCode,
    AiDraft? ServiceRequestDraft,
    MemoryCandidate? MemoryCandidate);

public sealed record DraftServiceRequestCommand(
    Guid ElderId,
    string SessionId,
    string Input);

public sealed record SummarizeVisitCommand(
    Guid ElderId,
    Guid VisitId,
    string SessionId,
    string RawVisitNote);

public interface IAiCareService
{
    Task<OperationResult<AiChatResult>> ChatAsync(
        AiChatCommand command,
        ActorContext actor,
        CancellationToken cancellationToken);

    Task<OperationResult<AiDraft>> DraftServiceRequestAsync(
        DraftServiceRequestCommand command,
        ActorContext actor,
        CancellationToken cancellationToken);

    Task<OperationResult<AiDraft>> SummarizeVisitAsync(
        SummarizeVisitCommand command,
        ActorContext actor,
        CancellationToken cancellationToken);

    Task<OperationResult<AiDraft>> ConfirmDraftAsync(
        Guid draftId,
        ActorContext actor,
        CancellationToken cancellationToken);

    Task<OperationResult<MemoryCandidate>> ConfirmMemoryAsync(
        Guid candidateId,
        ActorContext actor,
        CancellationToken cancellationToken);

    Task<OperationResult<bool>> DeleteMemoryAsync(
        Guid memoryId,
        ActorContext actor,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MemoryCandidate>> ListMemoriesAsync(
        ActorContext actor,
        CancellationToken cancellationToken);
}
