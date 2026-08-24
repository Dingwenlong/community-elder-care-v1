namespace CommunityElderCare.Infrastructure.Ai;

public sealed class CloudLlmOptions
{
    public const string SectionName = "Ai:CloudLlm";

    public string? BaseUrl { get; init; }
    public string? Model { get; init; }
    public string? ApiKey { get; init; }

    public bool IsConfigured =>
        Uri.TryCreate(BaseUrl, UriKind.Absolute, out _) &&
        !string.IsNullOrWhiteSpace(Model) &&
        !string.IsNullOrWhiteSpace(ApiKey);
}
