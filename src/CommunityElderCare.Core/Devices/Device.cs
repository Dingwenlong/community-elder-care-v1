namespace CommunityElderCare.Core.Devices;

public sealed class Device
{
    private Device()
    {
    }

    private Device(
        Guid id,
        Guid elderId,
        string displayName,
        string? tokenHash,
        DateTimeOffset registeredAt)
    {
        Id = id;
        ElderId = elderId;
        DisplayName = displayName;
        TokenHash = tokenHash;
        RegisteredAt = registeredAt;
        IsEnabled = true;
        IsDemoData = true;
    }

    public Guid Id { get; private set; }
    public Guid ElderId { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public string? TokenHash { get; private set; }
    public DateTimeOffset RegisteredAt { get; private set; }
    public void SetEnabled(bool enabled) => IsEnabled = enabled;

    public bool IsEnabled { get; private set; }
    public bool IsDemoData { get; private set; } = true;

    public static Device Register(
        Guid id,
        Guid elderId,
        string displayName,
        string? tokenHash,
        DateTimeOffset registeredAt)
    {
        if (id == Guid.Empty || elderId == Guid.Empty)
        {
            throw new ArgumentException("Device and elder IDs are required.");
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        if (tokenHash is not null && tokenHash.Length != 64)
        {
            throw new ArgumentException("Device token hash must be a SHA-256 hex value.", nameof(tokenHash));
        }

        return new Device(
            id,
            elderId,
            displayName.Trim(),
            tokenHash?.ToLowerInvariant(),
            registeredAt);
    }

    public void BindProcessTokenHash(string? tokenHash)
    {
        if (tokenHash is not null && tokenHash.Length != 64)
        {
            throw new ArgumentException("Device token hash must be a SHA-256 hex value.", nameof(tokenHash));
        }
        TokenHash = tokenHash?.ToLowerInvariant();
    }
}

public static class DemoDeviceIds
{
    public static Guid MainSosDevice { get; } =
        Guid.Parse("77777777-7777-7777-7777-777777777701");
}
