using CommunityElderCare.Core.Common;
using CommunityElderCare.Core.Identity;

namespace CommunityElderCare.Core.CareWork;

public sealed record CreateServiceOrderCommand(
    Guid CareEventId,
    string ServiceType,
    string ScheduledWindow,
    string ContactInstruction,
    Guid AssignedWorkerUserId,
    bool IsMandatory);

public sealed record ServiceWorkerOrderView(
    ServiceOrder Order,
    string ElderDisplayName);

public interface IServiceOrderService
{
    Task<OperationResult<ServiceWorkerOrderView>> CreateAsync(
        CreateServiceOrderCommand command,
        ActorContext actor,
        CancellationToken cancellationToken);

    Task<OperationResult<ServiceWorkerOrderView>> AcceptAsync(
        Guid orderId,
        ActorContext actor,
        CancellationToken cancellationToken);

    Task<OperationResult<ServiceWorkerOrderView>> CompleteAsync(
        Guid orderId,
        string result,
        ActorContext actor,
        CancellationToken cancellationToken);
}
