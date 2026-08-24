namespace CommunityElderCare.Core.CareEvents;

public enum CareEventCategory
{
    SafetyHealth,
    GeneralService,
}

public enum CareEventLevel
{
    GeneralService,
    NeedsConfirmation,
    Emergency,
}

public enum CareEventStatus
{
    PendingConfirmation,
    Accepted,
    InProgress,
    Resolved,
    FollowUpPending,
    Closed,
    FalseAlarm,
    UnableToConfirm,
}

public enum CareEventSource
{
    CheckIn,
    ElderHelp,
    FamilyReport,
    StaffVisit,
    Device,
    AiCue,
}

public enum CareEventActorKind
{
    Staff,
    Elder,
    Family,
    Ai,
    Device,
    Background,
}

public enum CareEventTrigger
{
    ExplicitSos,
    DangerCue,
    MissedCheckIn,
    DeviceAnomaly,
    LifeServiceNeed,
    FamilyConcern,
    StaffObservation,
}
