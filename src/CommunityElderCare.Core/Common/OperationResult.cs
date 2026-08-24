namespace CommunityElderCare.Core.Common;

public sealed record OperationResult<T>(
    bool IsSuccess,
    T? Value,
    string? ErrorCode,
    string? ErrorMessage);
