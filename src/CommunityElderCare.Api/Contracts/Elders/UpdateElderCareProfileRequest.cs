namespace CommunityElderCare.Api.Contracts.Elders;

public sealed record UpdateElderCareProfileRequest(
    string AttentionLevel,
    IReadOnlyList<HealthRiskInput> HealthRisks,
    IReadOnlyList<ServiceNeedInput> ServiceNeeds,
    IReadOnlyList<EmergencyContactInput> EmergencyContacts,
    string Reason);

public sealed record HealthRiskInput(string Code, string DemoLabel);

public sealed record ServiceNeedInput(string Code, string DemoLabel);

public sealed record EmergencyContactInput(
    string DemoName,
    string Relationship,
    string PhoneNumber,
    int ContactOrder);
