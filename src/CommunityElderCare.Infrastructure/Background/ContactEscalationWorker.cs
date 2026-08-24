using CommunityElderCare.Core.CareEvents;
using CommunityElderCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CommunityElderCare.Infrastructure.Background;

public sealed class ContactEscalationWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    EscalationPolicy escalationPolicy,
    ILogger<ContactEscalationWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await RunOnceAsync(stoppingToken);
            await Task.Delay(PollInterval, timeProvider, stoppingToken);
        }
    }

    public async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var recorder = scope.ServiceProvider.GetRequiredService<BackgroundJobRecorder>();
        var run = await recorder.StartAsync(nameof(ContactEscalationWorker), 0, cancellationToken);
        try
        {
        var dbContext = scope.ServiceProvider.GetRequiredService<CommunityCareDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<ICareEventService>();
        var now = timeProvider.GetUtcNow();
        var candidates = await dbContext.CareEvents.AsNoTracking().ToListAsync(cancellationToken);

        foreach (var candidate in candidates.Where(item => item.Status is
                     CareEventStatus.PendingConfirmation or
                     CareEventStatus.Accepted or
                     CareEventStatus.InProgress or
                     CareEventStatus.UnableToConfirm))
        {
            var dueActions = escalationPolicy.GetDueActions(
                candidate.Level,
                candidate.CreatedAt,
                now);
            if (candidate.Status == CareEventStatus.UnableToConfirm)
            {
                dueActions = dueActions
                    .Where(action => action == EscalationAction.Reassign)
                    .ToList();
            }
            foreach (var action in dueActions)
            {
                var result = await service.EscalateAsync(
                    candidate.Id,
                    action,
                    now,
                    cancellationToken);
                if (!result.IsSuccess)
                {
                    logger.LogWarning(
                        "Escalation {Action} for care event {CareEventId} failed: {ErrorCode}",
                        action,
                        candidate.Id,
                        result.ErrorCode);
                }
            }
        }
            await recorder.CompleteAsync(run, succeeded: true, exception: null, cancellationToken);
        }
        catch (Exception exception)
        {
            await recorder.CompleteAsync(run, succeeded: false, exception, CancellationToken.None);
            throw;
        }
    }
}
