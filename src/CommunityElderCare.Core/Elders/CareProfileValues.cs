namespace CommunityElderCare.Core.Elders;

public sealed record HealthRiskValue(string Code, string DemoLabel);

public sealed record ServiceNeedValue(string Code, string DemoLabel);

public sealed record EmergencyContactValue(
    string DemoName,
    string Relationship,
    string PhoneNumber,
    int ContactOrder);
