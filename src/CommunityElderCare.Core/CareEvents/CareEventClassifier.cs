namespace CommunityElderCare.Core.CareEvents;

public sealed record CareEventClassification(
    CareEventCategory Category,
    CareEventLevel Level);

public static class CareEventClassifier
{
    public static CareEventClassification Classify(CareEventTrigger trigger) => trigger switch
    {
        CareEventTrigger.ExplicitSos or CareEventTrigger.DangerCue =>
            new(CareEventCategory.SafetyHealth, CareEventLevel.Emergency),
        CareEventTrigger.MissedCheckIn or
        CareEventTrigger.DeviceAnomaly or
        CareEventTrigger.FamilyConcern or
        CareEventTrigger.StaffObservation =>
            new(CareEventCategory.SafetyHealth, CareEventLevel.NeedsConfirmation),
        CareEventTrigger.LifeServiceNeed =>
            new(CareEventCategory.GeneralService, CareEventLevel.GeneralService),
        _ => throw new ArgumentOutOfRangeException(nameof(trigger), trigger, "Unknown care-event trigger."),
    };
}
