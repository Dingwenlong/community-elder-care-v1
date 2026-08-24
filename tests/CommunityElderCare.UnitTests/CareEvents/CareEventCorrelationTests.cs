using CommunityElderCare.Core.CareEvents;

namespace CommunityElderCare.UnitTests.CareEvents;

public sealed class CareEventCorrelationTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 24, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid ElderId = Guid.Parse("11111111-1111-1111-1111-111111111101");

    [Theory]
    [InlineData(CareEventTrigger.ExplicitSos, CareEventCategory.SafetyHealth, CareEventLevel.Emergency)]
    [InlineData(CareEventTrigger.DangerCue, CareEventCategory.SafetyHealth, CareEventLevel.Emergency)]
    [InlineData(CareEventTrigger.MissedCheckIn, CareEventCategory.SafetyHealth, CareEventLevel.NeedsConfirmation)]
    [InlineData(CareEventTrigger.DeviceAnomaly, CareEventCategory.SafetyHealth, CareEventLevel.NeedsConfirmation)]
    [InlineData(CareEventTrigger.LifeServiceNeed, CareEventCategory.GeneralService, CareEventLevel.GeneralService)]
    public void Structured_trigger_has_a_fixed_classification(
        CareEventTrigger trigger,
        CareEventCategory category,
        CareEventLevel level)
    {
        var result = CareEventClassifier.Classify(trigger);

        Assert.Equal(category, result.Category);
        Assert.Equal(level, result.Level);
    }

    [Fact]
    public void Device_signal_joins_recent_open_safety_event_only()
    {
        var existing = CreateEvent(
            CareEventCategory.SafetyHealth,
            CareEventLevel.NeedsConfirmation,
            Now.AddMinutes(-10));
        var correlator = new CareEventCorrelator();

        var match = correlator.FindMatch([existing], ElderId, Now);

        Assert.Equal(existing.Id, match);
        Assert.Null(correlator.FindMatch(
            [CreateEvent(CareEventCategory.GeneralService, CareEventLevel.GeneralService, Now.AddMinutes(-10))],
            ElderId,
            Now));
    }

    [Fact]
    public void Correlation_window_includes_exactly_thirty_minutes_but_nothing_older()
    {
        var boundary = CreateEvent(
            CareEventCategory.SafetyHealth,
            CareEventLevel.NeedsConfirmation,
            Now.AddMinutes(-30));
        var older = CreateEvent(
            CareEventCategory.SafetyHealth,
            CareEventLevel.NeedsConfirmation,
            Now.AddMinutes(-30).AddTicks(-1));
        var correlator = new CareEventCorrelator();

        Assert.Equal(boundary.Id, correlator.FindMatch([boundary], ElderId, Now));
        Assert.Null(correlator.FindMatch([older], ElderId, Now));
    }

    [Fact]
    public void Closed_safety_event_is_not_a_correlation_target()
    {
        var closed = CreateEvent(
            CareEventCategory.SafetyHealth,
            CareEventLevel.NeedsConfirmation,
            Now.AddMinutes(-5));
        var transition = closed.TryTransition(
            CareEventStatus.FalseAlarm,
            CareEventActorKind.Staff,
            Guid.NewGuid(),
            "已核实为演示误报",
            resolution: null,
            Now);

        Assert.True(transition.IsAllowed);
        Assert.Null(new CareEventCorrelator().FindMatch([closed], ElderId, Now));
    }

    private static CareEvent CreateEvent(
        CareEventCategory category,
        CareEventLevel level,
        DateTimeOffset occurredAt) =>
        CareEvent.Create(
            Guid.NewGuid(),
            ElderId,
            category,
            level,
            CareEventSource.Device,
            $"device:{Guid.NewGuid():N}",
            "演示安全信号",
            occurredAt,
            "A01:care");
}
