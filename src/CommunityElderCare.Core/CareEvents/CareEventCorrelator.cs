namespace CommunityElderCare.Core.CareEvents;

public sealed class CareEventCorrelator
{
    public static TimeSpan CorrelationWindow { get; } = TimeSpan.FromMinutes(30);

    public Guid? FindMatch(
        IEnumerable<CareEvent> openEvents,
        Guid elderId,
        DateTimeOffset signalAt)
    {
        var windowStart = signalAt - CorrelationWindow;
        return openEvents
            .Where(careEvent =>
                careEvent.ElderId == elderId &&
                careEvent.Category == CareEventCategory.SafetyHealth &&
                !CareEventStateMachine.IsTerminal(careEvent.Status) &&
                careEvent.LastActivityAt >= windowStart &&
                careEvent.LastActivityAt <= signalAt)
            .OrderByDescending(careEvent => careEvent.LastActivityAt)
            .Select(careEvent => (Guid?)careEvent.Id)
            .FirstOrDefault();
    }
}
