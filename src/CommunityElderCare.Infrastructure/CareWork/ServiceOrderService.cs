using CommunityElderCare.Core.CareEvents;
using CommunityElderCare.Core.CareWork;
using CommunityElderCare.Core.Common;
using CommunityElderCare.Core.Identity;
using CommunityElderCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityElderCare.Infrastructure.CareWork;

public sealed class ServiceOrderService(
    CommunityCareDbContext dbContext,
    TimeProvider timeProvider) : IServiceOrderService
{
    public async Task<OperationResult<ServiceWorkerOrderView>> CreateAsync(
        CreateServiceOrderCommand command,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        var careEvent = await LoadEventAsync(command.CareEventId, cancellationToken);
        if (careEvent is null)
        {
            return Failure("NOT_FOUND", "Care event not found.");
        }
        if (actor.Role != DemoRole.CommunityStaff || careEvent.CurrentOwnerUserId != actor.UserId)
        {
            return Failure("FORBIDDEN_SCOPE", "Only the current community owner can create an order.");
        }
        if (careEvent.Status is not CareEventStatus.Accepted and not CareEventStatus.InProgress)
        {
            return Failure("INVALID_EVENT_STATUS", "Order requires an accepted or in-progress event.");
        }
        var validWorker = await dbContext.UserAccounts.AsNoTracking().AnyAsync(
            account =>
                account.Id == command.AssignedWorkerUserId &&
                account.Role == DemoRole.ServiceWorker &&
                account.ElderId == careEvent.ElderId,
            cancellationToken);
        if (!validWorker)
        {
            return Failure("INVALID_ASSIGNEE", "Order assignee must be the elder-scoped service worker.");
        }

        var now = timeProvider.GetUtcNow();
        var create = ServiceOrder.Create(
            Guid.NewGuid(),
            careEvent.Id,
            careEvent.ElderId,
            command.ServiceType,
            command.ScheduledWindow,
            command.ContactInstruction,
            command.AssignedWorkerUserId,
            command.IsMandatory,
            now);
        if (!create.IsSuccess)
        {
            return Failure(create.ErrorCode!, create.ErrorMessage!);
        }

        var order = create.Value!;
        dbContext.ServiceOrders.Add(order);
        if (order.IsMandatory)
        {
            careEvent.SetWorkState(
                hasIncompleteMandatoryTask: true,
                careEvent.RequiresFollowUp,
                careEvent.IsFollowUpCompleted);
        }
        AddEvidence(
            careEvent,
            "ServiceOrderCreated",
            $"已创建演示服务工单：{order.ServiceType}，{order.ScheduledWindow}",
            now,
            $"service-order:{order.Id:N}:created");
        await dbContext.SaveChangesAsync(cancellationToken);
        return await SuccessViewAsync(order, cancellationToken);
    }

    public async Task<OperationResult<ServiceWorkerOrderView>> AcceptAsync(
        Guid orderId,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        var order = await dbContext.ServiceOrders.SingleOrDefaultAsync(
            item => item.Id == orderId,
            cancellationToken);
        if (order is null)
        {
            return Failure("NOT_FOUND", "Service order not found.");
        }
        var careEvent = await LoadEventAsync(order.CareEventId, cancellationToken);
        if (careEvent is null)
        {
            return Failure("NOT_FOUND", "Care event not found.");
        }

        var now = timeProvider.GetUtcNow();
        var accept = order.Accept(actor, now);
        if (!accept.IsSuccess)
        {
            return Failure(accept.ErrorCode!, accept.ErrorMessage!);
        }
        var eventTransition = EnsureEventInProgress(careEvent, actor, now, "服务人员已接单");
        if (!eventTransition.IsSuccess)
        {
            return Failure(eventTransition.ErrorCode!, eventTransition.ErrorMessage!);
        }

        AddEvidence(
            careEvent,
            "ServiceOrderAccepted",
            $"服务人员已接收演示工单：{order.ServiceType}",
            now,
            $"service-order:{order.Id:N}:accepted");
        await dbContext.SaveChangesAsync(cancellationToken);
        return await SuccessViewAsync(order, cancellationToken);
    }

    public async Task<OperationResult<ServiceWorkerOrderView>> CompleteAsync(
        Guid orderId,
        string result,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        var order = await dbContext.ServiceOrders.SingleOrDefaultAsync(
            item => item.Id == orderId,
            cancellationToken);
        if (order is null)
        {
            return Failure("NOT_FOUND", "Service order not found.");
        }
        var careEvent = await LoadEventAsync(order.CareEventId, cancellationToken);
        if (careEvent is null)
        {
            return Failure("NOT_FOUND", "Care event not found.");
        }

        var now = timeProvider.GetUtcNow();
        var complete = order.Complete(actor, result, now);
        if (!complete.IsSuccess)
        {
            return Failure(complete.ErrorCode!, complete.ErrorMessage!);
        }
        var eventTransition = EnsureEventInProgress(careEvent, actor, now, "服务工单开始处理");
        if (!eventTransition.IsSuccess)
        {
            return Failure(eventTransition.ErrorCode!, eventTransition.ErrorMessage!);
        }

        await RefreshMandatoryWorkStateAsync(careEvent, cancellationToken);
        AddEvidence(
            careEvent,
            "ServiceOrderCompleted",
            order.Result!,
            now,
            $"service-order:{order.Id:N}:completed");
        await dbContext.SaveChangesAsync(cancellationToken);
        return await SuccessViewAsync(order, cancellationToken);
    }

    private OperationResult<bool> EnsureEventInProgress(
        CareEvent careEvent,
        ActorContext actor,
        DateTimeOffset now,
        string reason)
    {
        if (careEvent.Status == CareEventStatus.InProgress)
        {
            return new(true, true, null, null);
        }
        if (careEvent.Status != CareEventStatus.Accepted)
        {
            return new(false, false, "INVALID_EVENT_STATUS", "Order cannot start for this event status.");
        }

        var transition = careEvent.TryTransition(
            CareEventStatus.InProgress,
            CareEventActorKind.Staff,
            actor.UserId,
            reason,
            resolution: null,
            now);
        if (!transition.IsAllowed)
        {
            return new(false, false, transition.ErrorCode, transition.ErrorMessage);
        }
        dbContext.CareEventTransitions.Add(careEvent.Transitions.Last());
        return new(true, true, null, null);
    }

    private async Task RefreshMandatoryWorkStateAsync(
        CareEvent careEvent,
        CancellationToken cancellationToken)
    {
        var visits = await dbContext.VisitTasks
            .Where(item => item.CareEventId == careEvent.Id && item.IsMandatory)
            .ToListAsync(cancellationToken);
        var orders = await dbContext.ServiceOrders
            .Where(item => item.CareEventId == careEvent.Id && item.IsMandatory)
            .ToListAsync(cancellationToken);
        var incomplete = visits.Any(item => item.Status is not WorkStatus.Completed and not WorkStatus.Cancelled) ||
            orders.Any(item => item.Status is not WorkStatus.Completed and not WorkStatus.Cancelled);
        careEvent.SetWorkState(
            incomplete,
            careEvent.RequiresFollowUp,
            careEvent.IsFollowUpCompleted);
    }

    private async Task<OperationResult<ServiceWorkerOrderView>> SuccessViewAsync(
        ServiceOrder order,
        CancellationToken cancellationToken)
    {
        var displayName = await dbContext.ElderProfiles.AsNoTracking()
            .Where(profile => profile.Id == order.ElderId)
            .Select(profile => profile.DemoDisplayName)
            .SingleAsync(cancellationToken);
        return new(true, new(order, displayName), null, null);
    }

    private Task<CareEvent?> LoadEventAsync(Guid eventId, CancellationToken cancellationToken) =>
        dbContext.CareEvents
            .Include(item => item.Evidence)
            .Include(item => item.Transitions)
            .AsSplitQuery()
            .SingleOrDefaultAsync(item => item.Id == eventId, cancellationToken);

    private void AddEvidence(
        CareEvent careEvent,
        string kind,
        string summary,
        DateTimeOffset now,
        string sourceEventId)
    {
        var id = Guid.NewGuid();
        careEvent.AddEvidence(id, kind, summary, now, now, sourceEventId);
        dbContext.CareEventEvidence.Add(careEvent.Evidence.Single(item => item.Id == id));
    }

    private static OperationResult<ServiceWorkerOrderView> Failure(string code, string message) =>
        new(false, null, code, message);
}
