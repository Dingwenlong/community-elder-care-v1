using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace CommunityElderCare.Infrastructure.Ai;

public sealed class OpenAiCompatibleLlmClient(
    HttpClient httpClient,
    CloudLlmOptions options) : ICloudLlmClient
{
    public async Task<string> CompleteJsonAsync(
        IReadOnlyList<LlmMessage> messages,
        string schemaName,
        CancellationToken cancellationToken)
    {
        if (!options.IsConfigured)
        {
            throw new InvalidOperationException("AI_NOT_CONFIGURED");
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{options.BaseUrl!.TrimEnd('/')}/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        request.Content = JsonContent.Create(new
        {
            model = options.Model,
            messages = messages.Select(message => new
            {
                role = message.Role,
                content = message.Content,
            }),
            response_format = new { type = "json_object" },
            temperature = 0.2,
            metadata = new { schema = schemaName },
        });

        using var response = await httpClient.SendAsync(request, timeout.Token);
        response.EnsureSuccessStatusCode();
        using var document = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(timeout.Token),
            cancellationToken: timeout.Token);
        var choices = document.RootElement.GetProperty("choices");
        if (choices.GetArrayLength() == 0)
        {
            throw new InvalidDataException("AI_EMPTY_CHOICES");
        }
        var content = choices[0].GetProperty("message").GetProperty("content");
        if (content.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(content.GetString()))
        {
            throw new InvalidDataException("AI_EMPTY_CONTENT");
        }
        return content.GetString()!;
    }
}
