namespace CommunityElderCare.Core.Identity;

public interface IAccessPolicy
{
    Task<bool> CanReadAsync(
        ActorContext actor,
        Guid elderId,
        ConsentField field,
        CancellationToken cancellationToken);

    Task<bool> CanUpdateCareProfileAsync(
        ActorContext actor,
        Guid elderId,
        CancellationToken cancellationToken);
}
