namespace CommunityElderCare.Core.Identity;

public sealed class UserAccount
{
    private UserAccount()
    {
    }

    public UserAccount(
        Guid id,
        string username,
        DemoRole role,
        Guid? elderId,
        string? areaCode,
        Guid? assignedTaskId)
    {
        Id = id;
        Username = username;
        Role = role;
        ElderId = elderId;
        AreaCode = areaCode;
        AssignedTaskId = assignedTaskId;
        IsDemoData = true;
    }

    public Guid Id { get; private set; }

    public string Username { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;

    public void InitializeOperationsProfile(string displayName, string? areaCode)
    {
        if (string.IsNullOrWhiteSpace(DisplayName)) DisplayName = displayName;
        if (Role == DemoRole.ServiceWorker && AreaCode is null) AreaCode = areaCode;
    }

    public string PasswordHash { get; private set; } = string.Empty;

    public DemoRole Role { get; private set; }

    public Guid? ElderId { get; private set; }

    public string? AreaCode { get; private set; }

    public Guid? AssignedTaskId { get; private set; }

    public bool IsDemoData { get; private set; } = true;

    public void SetPasswordHash(string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        PasswordHash = passwordHash;
    }
}
