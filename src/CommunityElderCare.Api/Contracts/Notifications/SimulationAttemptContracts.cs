using CommunityElderCare.Core.Common;

namespace CommunityElderCare.Api.Contracts.Notifications;

public sealed record SimulationAttemptRequest(
    Guid RequestId,
    SimulationChannel Channel,
    string RecipientRole,
    bool SimulateFailure);

public sealed record SimulationAttemptResponse(
    Guid AttemptId,
    Guid CareEventId,
    Guid RequestId,
    SimulationChannel Channel,
    string RecipientRole,
    DateTimeOffset AttemptedAt,
    string Outcome,
    bool IsSimulation,
    bool IsDuplicate);
