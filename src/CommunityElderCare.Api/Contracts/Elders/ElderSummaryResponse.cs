namespace CommunityElderCare.Api.Contracts.Elders;

public sealed record ElderSummaryResponse(
    Guid Id,
    string DemoDisplayName,
    string AreaCode,
    string AttentionLevel,
    DateTimeOffset NextCheckInDueAt,
    bool IsDemoData);

public sealed record ElderDetailResponse(
    Guid Id,
    string DemoDisplayName,
    DateOnly BirthDate,
    string AreaCode,
    string AttentionLevel,
    DateTimeOffset NextCheckInDueAt,
    bool IsDemoData,
    IReadOnlyList<HealthRiskResponse> HealthRisks,
    IReadOnlyList<ServiceNeedResponse> ServiceNeeds,
    IReadOnlyList<EmergencyContactResponse> EmergencyContacts);

public sealed record HealthRiskResponse(string Code, string DemoLabel);

public sealed record ServiceNeedResponse(string Code, string DemoLabel);

public sealed record EmergencyContactResponse(
    string DemoName,
    string Relationship,
    string PhoneNumber,
    int ContactOrder);
