namespace CommunityElderCare.Core.Elders;

public interface IElderProfileQuery
{
    Task<IReadOnlyList<ElderProfile>> ListAsync(
        CareAttentionLevel? attentionLevel,
        string? areaCode,
        CancellationToken cancellationToken);

    Task<ElderProfile?> GetAsync(
        Guid elderId,
        string? areaCode,
        CancellationToken cancellationToken);
}
