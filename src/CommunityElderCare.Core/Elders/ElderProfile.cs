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

    public void ReplaceCareProfile(
        CareAttentionLevel attentionLevel,
        IReadOnlyCollection<HealthRiskValue> healthRisks,
        IReadOnlyCollection<ServiceNeedValue> serviceNeeds,
        IReadOnlyCollection<EmergencyContactValue> emergencyContacts)
    {
        ArgumentNullException.ThrowIfNull(healthRisks);
        ArgumentNullException.ThrowIfNull(serviceNeeds);
        ArgumentNullException.ThrowIfNull(emergencyContacts);
        if (healthRisks.Count == 0 || serviceNeeds.Count == 0 || emergencyContacts.Count == 0)
        {
            throw new ArgumentException("Care-profile collections cannot be empty.");
        }
        if (healthRisks.Any(value => string.IsNullOrWhiteSpace(value.Code) || string.IsNullOrWhiteSpace(value.DemoLabel)) ||
            serviceNeeds.Any(value => string.IsNullOrWhiteSpace(value.Code) || string.IsNullOrWhiteSpace(value.DemoLabel)) ||
            emergencyContacts.Any(value =>
                string.IsNullOrWhiteSpace(value.DemoName) ||
                string.IsNullOrWhiteSpace(value.Relationship) ||
                string.IsNullOrWhiteSpace(value.PhoneNumber)))
        {
            throw new ArgumentException("Care-profile values cannot be blank.");
        }

        var orderedContacts = emergencyContacts.OrderBy(value => value.ContactOrder).ToList();
        if (!orderedContacts.Select(value => value.ContactOrder).SequenceEqual(Enumerable.Range(1, orderedContacts.Count)))
        {
            throw new ArgumentException("Emergency-contact order must be contiguous and start at one.");
        }

        AttentionLevel = attentionLevel;
        _healthRisks.Clear();
        _serviceNeeds.Clear();
        _emergencyContacts.Clear();
        _healthRisks.AddRange(healthRisks.Select(value =>
            new HealthRisk(Guid.NewGuid(), Id, value.Code.Trim(), value.DemoLabel.Trim())));
        _serviceNeeds.AddRange(serviceNeeds.Select(value =>
            new ServiceNeed(Guid.NewGuid(), Id, value.Code.Trim(), value.DemoLabel.Trim())));
        _emergencyContacts.AddRange(orderedContacts.Select(value =>
            new EmergencyContact(
                Guid.NewGuid(),
                Id,
                value.DemoName.Trim(),
                value.Relationship.Trim(),
                value.PhoneNumber.Trim(),
                value.ContactOrder)));
    }
}
