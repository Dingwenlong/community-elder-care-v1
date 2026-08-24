namespace CommunityElderCare.Infrastructure.Ai;

public sealed record LlmMessage(string Role, string Content);

public interface ICloudLlmClient
{
    Task<string> CompleteJsonAsync(
        IReadOnlyList<LlmMessage> messages,
        string schemaName,
        CancellationToken cancellationToken);
}
