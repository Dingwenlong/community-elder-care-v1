using CommunityElderCare.Core.Common;
using CommunityElderCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityElderCare.Infrastructure.Background;

public sealed class BackgroundJobRecorder(
    CommunityCareDbContext dbContext,
    TimeProvider timeProvider)
{
    public async Task<BackgroundJobRun> StartAsync(
        string jobName,
        int retryCount,
        CancellationToken cancellationToken)
    {
        var run = BackgroundJobRun.Start(
            Guid.NewGuid(),
            jobName,
            timeProvider.GetUtcNow(),
            retryCount);
        dbContext.BackgroundJobRuns.Add(run);
        await dbContext.SaveChangesAsync(cancellationToken);
        return run;
    }

    public async Task CompleteAsync(
        BackgroundJobRun run,
        bool succeeded,
        Exception? exception,
        CancellationToken cancellationToken)
    {
        run.Complete(
            timeProvider.GetUtcNow(),
            succeeded ? BackgroundJobResult.Succeeded : BackgroundJobResult.Failed,
            exception is null ? null : Sanitize(exception));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string Sanitize(Exception exception) => exception switch
    {
        OperationCanceledException => "CANCELLED",
        DbUpdateException => "DATABASE_WRITE_FAILED",
        _ => $"UNEXPECTED_{exception.GetType().Name.ToUpperInvariant()}",
    };
}
