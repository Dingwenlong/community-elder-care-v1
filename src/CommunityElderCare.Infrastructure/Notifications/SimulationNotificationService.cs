using CommunityElderCare.Core.CareEvents;
using CommunityElderCare.Core.Common;
using CommunityElderCare.Core.Identity;
using CommunityElderCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityElderCare.Infrastructure.Notifications;

public sealed record RecordSimulationAttemptCommand(
    Guid CareEventId,
    Guid RequestId,
    SimulationChannel Channel,
    string RecipientRole,
    bool SimulateFailure);

public sealed record SimulationAttemptReceipt(
    NotificationAttempt Attempt,
    bool IsDuplicate);

public sealed class SimulationNotificationService(
    CommunityCareDbContext dbContext,
    TimeProvider timeProvider)
{
    public async Task<OperationResult<SimulationAttemptReceipt>> RecordAsync(
        RecordSimulationAttemptCommand command,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        if (command.RequestId == Guid.Empty || string.IsNullOrWhiteSpace(command.RecipientRole))
        {
            return Failure("INVALID_SIMULATION_ATTEMPT", "Request ID and recipient role are required.");
        }

        var careEvent = await dbContext.CareEvents
            .Include(candidate => candidate.ContactAttempts)
            .SingleOrDefaultAsync(candidate => candidate.Id == command.CareEventId, cancellationToken);
        if (careEvent is null)
        {
            return Failure("NOT_FOUND", "Care event was not found.");
        }
        var elderArea = await dbContext.ElderProfiles.AsNoTracking()
            .Where(elder => elder.Id == careEvent.ElderId)
            .Select(elder => elder.AreaCode)
            .SingleAsync(cancellationToken);
        if (actor.Role != DemoRole.CommunityStaff ||
            actor.AreaCode != elderArea ||
            careEvent.CurrentOwnerUserId != actor.UserId)
        {
            return Failure("FORBIDDEN_SCOPE", "Only the current in-area handler can record a simulation.");
        }

        var duplicate = await FindAsync(command.CareEventId, command.RequestId, cancellationToken);
        if (duplicate is not null)
        {
            return Success(duplicate, isDuplicate: true);
        }

        var attempt = NotificationAttempt.Create(
            Guid.NewGuid(),
            command.CareEventId,
            command.RequestId,
            command.Channel,
            command.RecipientRole,
            timeProvider.GetUtcNow(),
            command.SimulateFailure,
            actor.UserId);
        dbContext.NotificationAttempts.Add(attempt);
        var contactMapping = MapContact(command.Channel, command.RecipientRole);
        if (careEvent.AddContactAttempt(
                attempt.Id,
                $"notification:{command.RequestId:N}",
                contactMapping.Kind,
                contactMapping.TargetLabel,
                attempt.AttemptedAt,
                attempt.Outcome))
        {
            dbContext.ContactAttempts.Add(
                careEvent.ContactAttempts.Single(contact => contact.Id == attempt.Id));
        }
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return Success(attempt, isDuplicate: false);
        }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();
            var concurrent = await FindAsync(
                command.CareEventId,
                command.RequestId,
                cancellationToken);
            return concurrent is null
                ? Failure("PERSISTENCE_ERROR", "Simulation attempt could not be stored.")
                : Success(concurrent, isDuplicate: true);
        }
    }

    private Task<NotificationAttempt?> FindAsync(
        Guid careEventId,
        Guid requestId,
        CancellationToken cancellationToken) => dbContext.NotificationAttempts
        .AsNoTracking()
        .SingleOrDefaultAsync(
            attempt => attempt.CareEventId == careEventId && attempt.RequestId == requestId,
            cancellationToken);

    private static OperationResult<SimulationAttemptReceipt> Success(
        NotificationAttempt attempt,
        bool isDuplicate) => new(
        true,
        new SimulationAttemptReceipt(attempt, isDuplicate),
        null,
        null);

    private static OperationResult<SimulationAttemptReceipt> Failure(string code, string message) =>
        new(false, null, code, message);

    private static ContactMapping MapContact(SimulationChannel channel, string recipientRole) =>
        channel switch
        {
            SimulationChannel.InAppNotification =>
                new(ContactAttemptKind.ElderReminder, "老人端站内通知"),
            SimulationChannel.Sms =>
                new(ContactAttemptKind.EmergencyContact, $"{recipientRole} 模拟短信"),
            SimulationChannel.Phone =>
                new(ContactAttemptKind.PhoneConfirmation, $"{recipientRole} 模拟电话"),
            SimulationChannel.HomeVisit =>
                new(ContactAttemptKind.CommunityNotification, "社区模拟上门"),
            SimulationChannel.EmergencyTransport =>
                new(ContactAttemptKind.CommunityNotification, "120 模拟急救转运"),
            _ => throw new ArgumentOutOfRangeException(nameof(channel), channel, null),
        };

    private sealed record ContactMapping(ContactAttemptKind Kind, string TargetLabel);
}
