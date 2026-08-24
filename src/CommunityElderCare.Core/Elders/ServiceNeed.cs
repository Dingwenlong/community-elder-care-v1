namespace CommunityElderCare.Core.Elders;

public sealed class ServiceNeed
{
    private ServiceNeed()
    {
    }

    internal ServiceNeed(Guid id, Guid elderProfileId, string code, string demoLabel)
    {
        Id = id;
        ElderProfileId = elderProfileId;
        Code = code;
        DemoLabel = demoLabel;
    }

    public Guid Id { get; private set; }

    public Guid ElderProfileId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string DemoLabel { get; private set; } = string.Empty;
}
