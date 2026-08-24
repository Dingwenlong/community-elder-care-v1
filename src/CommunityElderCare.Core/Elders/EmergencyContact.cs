namespace CommunityElderCare.Core.Elders;

public sealed class EmergencyContact
{
    private EmergencyContact()
    {
    }

    internal EmergencyContact(
        Guid id,
        Guid elderProfileId,
        string demoName,
        string relationship,
        string phoneNumber,
        int contactOrder)
    {
        Id = id;
        ElderProfileId = elderProfileId;
        DemoName = demoName;
        Relationship = relationship;
        PhoneNumber = phoneNumber;
        ContactOrder = contactOrder;
    }

    public Guid Id { get; private set; }

    public Guid ElderProfileId { get; private set; }

    public string DemoName { get; private set; } = string.Empty;

    public string Relationship { get; private set; } = string.Empty;

    public string PhoneNumber { get; private set; } = string.Empty;

    public int ContactOrder { get; private set; }
}
