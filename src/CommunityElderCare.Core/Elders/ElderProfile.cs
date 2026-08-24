namespace CommunityElderCare.Core.Elders;

public sealed class ElderProfile
{
    private readonly List<HealthRisk> _healthRisks = [];
    private readonly List<ServiceNeed> _serviceNeeds = [];
    private readonly List<EmergencyContact> _emergencyContacts = [];

    private ElderProfile()
    {
    }

    internal ElderProfile(
        Guid id,
        string demoDisplayName,
        DateOnly birthDate,
        string areaCode,
        CareAttentionLevel attentionLevel,
        DateTimeOffset nextCheckInDueAt)
    {
        Id = id;
        DemoDisplayName = demoDisplayName;
        BirthDate = birthDate;
        AreaCode = areaCode;
        AttentionLevel = attentionLevel;
        NextCheckInDueAt = nextCheckInDueAt;
        IsDemoData = true;
    }

    public Guid Id { get; private set; }

    public string DemoDisplayName { get; private set; } = string.Empty;

    public DateOnly BirthDate { get; private set; }

    public string AreaCode { get; private set; } = string.Empty;

    public CareAttentionLevel AttentionLevel { get; private set; }

    public DateTimeOffset NextCheckInDueAt { get; private set; }

    public bool IsDemoData { get; private set; } = true;

    public IReadOnlyCollection<HealthRisk> HealthRisks => _healthRisks;

    public IReadOnlyCollection<ServiceNeed> ServiceNeeds => _serviceNeeds;

    public IReadOnlyCollection<EmergencyContact> EmergencyContacts => _emergencyContacts;

    internal void AddHealthRisk(HealthRisk risk) => _healthRisks.Add(risk);

    internal void AddServiceNeed(ServiceNeed need) => _serviceNeeds.Add(need);

    internal void AddEmergencyContact(EmergencyContact contact) => _emergencyContacts.Add(contact);
}
