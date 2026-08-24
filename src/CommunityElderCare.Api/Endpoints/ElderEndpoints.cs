using System.Text.RegularExpressions;
using CommunityElderCare.Api.Contracts.Elders;
using CommunityElderCare.Api.Identity;
using CommunityElderCare.Core.Elders;
using CommunityElderCare.Core.Identity;
using CommunityElderCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityElderCare.Api.Endpoints;

public static partial class ElderEndpoints
{
    [GeneratedRegex("^1990000[0-9]{4}$", RegexOptions.CultureInvariant)]
    private static partial Regex DemoPhonePattern();

    public static IEndpointRouteBuilder MapElderEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/elders").RequireAuthorization();
        group.MapGet("", ListAsync);
        group.MapGet("/{elderId:guid}", GetAsync);
        group.MapPut("/{elderId:guid}/care-profile", UpdateCareProfileAsync);
        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        string? attentionLevel,
        HttpContext httpContext,
        IElderProfileQuery query,
        CancellationToken cancellationToken)
    {
        if (!TryParseAttentionLevel(attentionLevel, out var parsedAttentionLevel))
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                "INVALID_ATTENTION_LEVEL",
                "Invalid attention level");
        }

        var actor = httpContext.User.GetActorContext();
        var areaCode = actor.Role == DemoRole.CommunityStaff ? actor.AreaCode : null;
        var profiles = await query.ListAsync(parsedAttentionLevel, areaCode, cancellationToken);
        if (actor.Role is DemoRole.Elder or DemoRole.Family or DemoRole.ServiceWorker)
        {
            profiles = profiles.Where(profile => profile.Id == actor.ElderId).ToList();
        }

        return Results.Ok(profiles.Select(ToSummary).ToList());
    }

    private static async Task<IResult> GetAsync(
        Guid elderId,
        HttpContext httpContext,
        IElderProfileQuery query,
        IAccessPolicy accessPolicy,
        CancellationToken cancellationToken)
    {
        var profile = await query.GetAsync(elderId, areaCode: null, cancellationToken);
        if (profile is null)
        {
            return Results.NotFound();
        }

        var actor = httpContext.User.GetActorContext();
        return await BuildProjectionAsync(profile, actor, accessPolicy, cancellationToken);
    }

    private static async Task<IResult> UpdateCareProfileAsync(
        Guid elderId,
        UpdateElderCareProfileRequest request,
        HttpContext httpContext,
        CommunityCareDbContext dbContext,
        IAccessPolicy accessPolicy,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var actor = httpContext.User.GetActorContext();
        if (!await accessPolicy.CanUpdateCareProfileAsync(actor, elderId, cancellationToken))
        {
            return Problem(StatusCodes.Status403Forbidden, "FORBIDDEN_SCOPE", "Forbidden scope");
        }
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return Problem(StatusCodes.Status400BadRequest, "REASON_REQUIRED", "Reason required");
        }
        if (!TryValidateCareProfile(request, out var attentionLevel))
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                "INVALID_CARE_PROFILE",
                "Invalid care profile");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var profile = await dbContext.ElderProfiles
            .AsSplitQuery()
            .Include(candidate => candidate.HealthRisks)
            .Include(candidate => candidate.ServiceNeeds)
            .Include(candidate => candidate.EmergencyContacts)
            .SingleOrDefaultAsync(candidate => candidate.Id == elderId, cancellationToken);
        if (profile is null)
        {
            return Results.NotFound();
        }

        profile.ReplaceCareProfile(
            attentionLevel,
            request.HealthRisks.Select(value => new HealthRiskValue(value.Code, value.DemoLabel)).ToList(),
            request.ServiceNeeds.Select(value => new ServiceNeedValue(value.Code, value.DemoLabel)).ToList(),
            request.EmergencyContacts.Select(value => new EmergencyContactValue(
                value.DemoName,
                value.Relationship,
                value.PhoneNumber,
                value.ContactOrder)).ToList());
        dbContext.HealthRisks.AddRange(profile.HealthRisks);
        dbContext.ServiceNeeds.AddRange(profile.ServiceNeeds);
        dbContext.EmergencyContacts.AddRange(profile.EmergencyContacts);
        dbContext.AccessAuditRecords.Add(new AccessAuditRecord(
            Guid.NewGuid(),
            "CARE_PROFILE_UPDATED",
            actor.UserId,
            elderId,
            request.Reason,
            timeProvider.GetUtcNow(),
            "AttentionLevel,HealthRisks,ServiceNeeds,EmergencyContacts"));
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await BuildProjectionAsync(profile, actor, accessPolicy, cancellationToken);
    }

    private static async Task<IResult> BuildProjectionAsync(
        ElderProfile profile,
        ActorContext actor,
        IAccessPolicy accessPolicy,
        CancellationToken cancellationToken)
    {
        var canReadRecent = await accessPolicy.CanReadAsync(
            actor, profile.Id, ConsentField.RecentStatus, cancellationToken);
        var canReadCareEvents = await accessPolicy.CanReadAsync(
            actor, profile.Id, ConsentField.CareEventSummary, cancellationToken);
        var canReadVisits = await accessPolicy.CanReadAsync(
            actor, profile.Id, ConsentField.VisitSummary, cancellationToken);
        var canReadReminders = await accessPolicy.CanReadAsync(
            actor, profile.Id, ConsentField.ReminderCompletion, cancellationToken);
        var canReadHealth = await accessPolicy.CanReadAsync(
            actor, profile.Id, ConsentField.HealthRiskSummary, cancellationToken);
        var canReadContacts = await accessPolicy.CanReadAsync(
            actor, profile.Id, ConsentField.EmergencyContact, cancellationToken);
        if (!(canReadRecent || canReadCareEvents || canReadVisits || canReadReminders || canReadHealth || canReadContacts))
        {
            var code = actor.Role == DemoRole.Family ? "CONSENT_REQUIRED" : "FORBIDDEN_SCOPE";
            return Problem(StatusCodes.Status403Forbidden, code, "Access denied");
        }

        var response = new Dictionary<string, object?>
        {
            ["id"] = profile.Id,
            ["demoDisplayName"] = profile.DemoDisplayName,
            ["isDemoData"] = profile.IsDemoData,
        };
        if (actor.Role is DemoRole.Elder or DemoRole.CommunityStaff)
        {
            response["birthDate"] = profile.BirthDate;
            response["areaCode"] = profile.AreaCode;
            response["nextCheckInDueAt"] = profile.NextCheckInDueAt;
            response["attentionLevel"] = profile.AttentionLevel.ToString();
        }
        else if (canReadHealth)
        {
            response["attentionLevel"] = profile.AttentionLevel.ToString();
        }
        if (canReadRecent)
        {
            response["recentStatus"] = new
            {
                state = "AwaitingDemoCheckIn",
                latestCheckInAt = (DateTimeOffset?)null,
            };
        }
        if (canReadCareEvents)
        {
            response["careEventSummary"] = new { activeCount = 0 };
        }
        if (canReadVisits)
        {
            response["visitSummary"] = new { latestVisitAt = (DateTimeOffset?)null };
        }
        if (canReadReminders)
        {
            response["reminderCompletion"] = new { completedToday = 0, totalToday = 0 };
        }
        if (canReadHealth)
        {
            response["healthRisks"] = profile.HealthRisks
                .Select(risk => new HealthRiskResponse(risk.Code, risk.DemoLabel))
                .ToList();
        }
        if (actor.Role is DemoRole.Elder or DemoRole.CommunityStaff)
        {
            response["serviceNeeds"] = profile.ServiceNeeds
                .Select(need => new ServiceNeedResponse(need.Code, need.DemoLabel))
                .ToList();
        }
        if (canReadContacts)
        {
            response["emergencyContacts"] = profile.EmergencyContacts
                .OrderBy(contact => contact.ContactOrder)
                .Select(contact => new EmergencyContactResponse(
                    contact.DemoName,
                    contact.Relationship,
                    contact.PhoneNumber,
                    contact.ContactOrder))
                .ToList();
        }

        return Results.Ok(response);
    }

    private static bool TryParseAttentionLevel(
        string? value,
        out CareAttentionLevel? attentionLevel)
    {
        attentionLevel = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }
        if (!Enum.TryParse<CareAttentionLevel>(value, ignoreCase: true, out var parsed))
        {
            return false;
        }

        attentionLevel = parsed;
        return true;
    }

    private static bool TryValidateCareProfile(
        UpdateElderCareProfileRequest request,
        out CareAttentionLevel attentionLevel)
    {
        if (!Enum.TryParse(request.AttentionLevel, ignoreCase: true, out attentionLevel) ||
            request.HealthRisks is null || request.HealthRisks.Count == 0 ||
            request.ServiceNeeds is null || request.ServiceNeeds.Count == 0 ||
            request.EmergencyContacts is null || request.EmergencyContacts.Count == 0 ||
            request.HealthRisks.Any(value => BlankOrTooLong(value.Code, 64) || BlankOrTooLong(value.DemoLabel, 128)) ||
            request.ServiceNeeds.Any(value => BlankOrTooLong(value.Code, 64) || BlankOrTooLong(value.DemoLabel, 128)) ||
            request.EmergencyContacts.Any(value =>
                BlankOrTooLong(value.DemoName, 64) ||
                BlankOrTooLong(value.Relationship, 32) ||
                !DemoPhonePattern().IsMatch(value.PhoneNumber ?? string.Empty)))
        {
            return false;
        }

        var orders = request.EmergencyContacts.Select(value => value.ContactOrder).Order().ToArray();
        return orders.SequenceEqual(Enumerable.Range(1, orders.Length));
    }

    private static bool BlankOrTooLong(string? value, int maxLength) =>
        string.IsNullOrWhiteSpace(value) || value.Length > maxLength;

    private static ElderSummaryResponse ToSummary(ElderProfile profile) => new(
        profile.Id,
        profile.DemoDisplayName,
        profile.AreaCode,
        profile.AttentionLevel.ToString(),
        profile.NextCheckInDueAt,
        profile.IsDemoData);

    private static IResult Problem(int statusCode, string code, string title) => Results.Problem(
        statusCode: statusCode,
        title: title,
        extensions: new Dictionary<string, object?> { ["code"] = code });
}
