namespace CommunityElderCare.Api.Contracts.Ai;

public sealed record ElderChatRequest(
    Guid ElderId,
    string SessionId,
    string Input);

public sealed record ServiceRequestDraftRequest(
    Guid ElderId,
    string SessionId,
    string Input);

public sealed record VisitSummaryDraftRequest(
    Guid ElderId,
    Guid VisitId,
    string SessionId,
    string RawVisitNote);

public sealed record DangerCueResponse(
    bool IsEmergency,
    bool NeedsConfirmation,
    string Code);

public sealed record AiChatResponse(
    string Reply,
    bool UsedFallback,
    DangerCueResponse DangerCue,
    Guid? CareEventId,
    string? RejectionCode,
    AiDraftResponse? ServiceRequestDraft,
    AiMemoryResponse? MemoryCandidate);

public sealed record AiDraftResponse(
    Guid Id,
    string Kind,
    string GeneratedText,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ConfirmedAt);

public sealed record AiMemoryResponse(
    Guid Id,
    string GeneratedText,
    bool IsConfirmed,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ConfirmedAt);
