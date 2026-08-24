using CommunityElderCare.Api.Contracts.Elders;
using CommunityElderCare.Core.Elders;

namespace CommunityElderCare.Api.Endpoints;

public static class ElderEndpoints
{
    private const string DemoAreaHeader = "X-Demo-Area-Code";

    public static IEndpointRouteBuilder MapElderEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/elders");
        group.MapGet("", ListAsync);
        group.MapGet("/{elderId:guid}", GetAsync);
        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        string? attentionLevel,
        HttpContext httpContext,
        IElderProfileQuery query,
        CancellationToken cancellationToken)
    {
        CareAttentionLevel? parsedAttentionLevel = null;
        if (!string.IsNullOrWhiteSpace(attentionLevel))
        {
            if (!Enum.TryParse<CareAttentionLevel>(attentionLevel, ignoreCase: true, out var parsed))
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid attention level",
                    extensions: new Dictionary<string, object?> { ["code"] = "invalid_attention_level" });
            }

            parsedAttentionLevel = parsed;
        }

        var profiles = await query.ListAsync(
            parsedAttentionLevel,
            ReadAreaCode(httpContext),
            cancellationToken);
        return Results.Ok(profiles.Select(ToSummary).ToList());
    }

    private static async Task<IResult> GetAsync(
        Guid elderId,
        HttpContext httpContext,
        IElderProfileQuery query,
        CancellationToken cancellationToken)
    {
        var profile = await query.GetAsync(elderId, ReadAreaCode(httpContext), cancellationToken);
        return profile is null ? Results.NotFound() : Results.Ok(ToDetail(profile));
    }

    private static string? ReadAreaCode(HttpContext context)
    {
        var value = context.Request.Headers[DemoAreaHeader].ToString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static ElderSummaryResponse ToSummary(ElderProfile profile) => new(
        profile.Id,
        profile.DemoDisplayName,
        profile.AreaCode,
        profile.AttentionLevel.ToString(),
        profile.NextCheckInDueAt,
        profile.IsDemoData);

    private static ElderDetailResponse ToDetail(ElderProfile profile) => new(
        profile.Id,
        profile.DemoDisplayName,
        profile.BirthDate,
        profile.AreaCode,
        profile.AttentionLevel.ToString(),
        profile.NextCheckInDueAt,
        profile.IsDemoData,
        profile.HealthRisks.Select(risk => new HealthRiskResponse(risk.Code, risk.DemoLabel)).ToList(),
        profile.ServiceNeeds.Select(need => new ServiceNeedResponse(need.Code, need.DemoLabel)).ToList(),
        profile.EmergencyContacts
            .OrderBy(contact => contact.ContactOrder)
            .Select(contact => new EmergencyContactResponse(
                contact.DemoName,
                contact.Relationship,
                contact.PhoneNumber,
                contact.ContactOrder))
            .ToList());
}
