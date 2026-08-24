using CommunityElderCare.Core.CareEvents;
using CommunityElderCare.Core.CheckIns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CommunityElderCare.Infrastructure.Background;

public sealed class MissedCheckInWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<MissedCheckInWorker> logger) : BackgroundService
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
        var run = await recorder.StartAsync(nameof(MissedCheckInWorker), 0, cancellationToken);
        try
        {
        var checkInService = scope.ServiceProvider.GetRequiredService<ICheckInService>();
        var careEventService = scope.ServiceProvider.GetRequiredService<ICareEventService>();
        var overdueItems = await checkInService.GetOverdueCheckInsAsync(
            timeProvider.GetUtcNow(),
            cancellationToken);

        foreach (var overdue in overdueItems)
        {
            var sourceEventId = $"missed-check-in:{overdue.ElderId:N}:{overdue.DueAt.UtcTicks}";
            var result = await careEventService.CreateAsync(
                new CreateCareEventCommand(
                    overdue.ElderId,
                    CareEventTrigger.MissedCheckIn,
                    CareEventSource.CheckIn,
                    sourceEventId,
                    "演示记录：老人未在计划时间内完成平安确认",
                    overdue.DueAt,
                    CareEventActorKind.Background),
                actor: null,
                cancellationToken);
            if (!result.IsSuccess)
            {
                logger.LogWarning(
                    "Missed check-in event {SourceEventId} was not created: {ErrorCode}",
                    sourceEventId,
                    result.ErrorCode);
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
