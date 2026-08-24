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

        var scope = await (
            from careEvent in dbContext.CareEvents.AsNoTracking()
            join elder in dbContext.ElderProfiles.AsNoTracking()
                on careEvent.ElderId equals elder.Id
            where careEvent.Id == command.CareEventId
            select new { careEvent.CurrentOwnerUserId, elder.AreaCode })
            .SingleOrDefaultAsync(cancellationToken);
        if (scope is null)
        {
            return Failure("NOT_FOUND", "Care event was not found.");
        }
        if (actor.Role != DemoRole.CommunityStaff ||
            actor.AreaCode != scope.AreaCode ||
            scope.CurrentOwnerUserId != actor.UserId)
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
}
