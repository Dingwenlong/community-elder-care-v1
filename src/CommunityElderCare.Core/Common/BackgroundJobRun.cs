namespace CommunityElderCare.Core.Common;

public enum BackgroundJobResult
{
    Running,
    Succeeded,
    Failed,
}

public sealed class BackgroundJobRun
{
    private BackgroundJobRun()
    {
    }

    private BackgroundJobRun(Guid id, string jobName, DateTimeOffset startedAt, int retryCount)
    {
        Id = id;
        JobName = jobName;
        StartedAt = startedAt;
        RetryCount = retryCount;
        Result = BackgroundJobResult.Running;
        IsDemoData = true;
    }

    public Guid Id { get; private set; }
    public string JobName { get; private set; } = string.Empty;
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? EndedAt { get; private set; }
    public BackgroundJobResult Result { get; private set; }
    public int RetryCount { get; private set; }
    public string? ErrorCode { get; private set; }
    public bool IsDemoData { get; private set; } = true;

    public static BackgroundJobRun Start(
        Guid id,
        string jobName,
        DateTimeOffset startedAt,
        int retryCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobName);
        ArgumentOutOfRangeException.ThrowIfNegative(retryCount);
        return new BackgroundJobRun(id, jobName.Trim(), startedAt, retryCount);
    }

    public void Complete(DateTimeOffset endedAt, BackgroundJobResult result, string? errorCode)
    {
        if (result == BackgroundJobResult.Running)
        {
            throw new ArgumentOutOfRangeException(nameof(result));
        }
        EndedAt = endedAt;
        Result = result;
        ErrorCode = string.IsNullOrWhiteSpace(errorCode) ? null : errorCode.Trim();
    }
}
