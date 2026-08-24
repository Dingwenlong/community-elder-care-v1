namespace CommunityElderCare.Api.Contracts.Auth;

public sealed record BreakGlassRequest(string Reason, int DurationMinutes = 15);

public sealed record BreakGlassResponse(
    Guid Id,
    Guid ElderId,
    Guid CareEventId,
    DateTimeOffset ExpiresAt,
    bool IsDemoData);
