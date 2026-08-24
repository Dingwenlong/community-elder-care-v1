using CommunityElderCare.Core.CareEvents;

namespace CommunityElderCare.UnitTests.CareEvents;

public sealed class CareEventStateMachineTests
{
    [Theory]
    [InlineData(CareEventStatus.PendingConfirmation, CareEventStatus.Accepted, true)]
    [InlineData(CareEventStatus.Accepted, CareEventStatus.InProgress, true)]
    [InlineData(CareEventStatus.InProgress, CareEventStatus.Resolved, true)]
    [InlineData(CareEventStatus.Resolved, CareEventStatus.FollowUpPending, true)]
    [InlineData(CareEventStatus.FollowUpPending, CareEventStatus.Closed, true)]
    [InlineData(CareEventStatus.UnableToConfirm, CareEventStatus.Closed, false)]
    [InlineData(CareEventStatus.PendingConfirmation, CareEventStatus.Closed, false)]
    public void Transition_matrix_is_enforced(
        CareEventStatus from,
        CareEventStatus to,
        bool allowed)
    {
        Assert.Equal(allowed, CareEventStateMachine.CanTransition(from, to));
    }

    [Fact]
    public void False_alarm_requires_a_reason()
    {
        var result = CareEventStateMachine.ValidateTransition(
            CareEventStatus.PendingConfirmation,
            CareEventStatus.FalseAlarm,
            CareEventActorKind.Staff,
            reason: "  ",
            CareEventClosureState.NotApplicable);

        Assert.False(result.IsAllowed);
        Assert.Equal("REASON_REQUIRED", result.ErrorCode);
    }

    [Fact]
    public void Unable_to_confirm_is_not_terminal()
    {
        Assert.False(CareEventStateMachine.IsTerminal(CareEventStatus.UnableToConfirm));
        Assert.True(CareEventStateMachine.CanTransition(
            CareEventStatus.UnableToConfirm,
            CareEventStatus.Accepted));
    }

    [Theory]
    [InlineData(CareEventActorKind.Ai)]
    [InlineData(CareEventActorKind.Device)]
    [InlineData(CareEventActorKind.Background)]
    public void Automated_actors_cannot_close(CareEventActorKind actorKind)
    {
        var result = CareEventStateMachine.ValidateTransition(
            CareEventStatus.Resolved,
            CareEventStatus.Closed,
            actorKind,
            reason: "演示事件已核实",
            new CareEventClosureState(
                HasCurrentStaffOwner: true,
                Resolution: "已完成处置",
                HasIncompleteMandatoryTask: false,
                RequiresFollowUp: false,
                IsFollowUpCompleted: false));

        Assert.False(result.IsAllowed);
        Assert.Equal("STAFF_CLOSE_REQUIRED", result.ErrorCode);
    }

    [Fact]
    public void Closing_guard_enforces_owner_resolution_tasks_and_follow_up()
    {
        var missingOwner = ValidateClose(new(
            HasCurrentStaffOwner: false,
            Resolution: "已完成处置",
            HasIncompleteMandatoryTask: false,
            RequiresFollowUp: false,
            IsFollowUpCompleted: false));
        var missingResolution = ValidateClose(new(
            HasCurrentStaffOwner: true,
            Resolution: " ",
            HasIncompleteMandatoryTask: false,
            RequiresFollowUp: false,
            IsFollowUpCompleted: false));
        var incompleteTask = ValidateClose(new(
            HasCurrentStaffOwner: true,
            Resolution: "已完成处置",
            HasIncompleteMandatoryTask: true,
            RequiresFollowUp: false,
            IsFollowUpCompleted: false));
        var incompleteFollowUp = ValidateClose(new(
            HasCurrentStaffOwner: true,
            Resolution: "已完成处置",
            HasIncompleteMandatoryTask: false,
            RequiresFollowUp: true,
            IsFollowUpCompleted: false));

        Assert.Equal("OWNER_REQUIRED", missingOwner.ErrorCode);
        Assert.Equal("RESOLUTION_REQUIRED", missingResolution.ErrorCode);
        Assert.Equal("MANDATORY_TASK_INCOMPLETE", incompleteTask.ErrorCode);
        Assert.Equal("FOLLOW_UP_INCOMPLETE", incompleteFollowUp.ErrorCode);
    }

    private static CareEventTransitionDecision ValidateClose(CareEventClosureState state) =>
        CareEventStateMachine.ValidateTransition(
            CareEventStatus.Resolved,
            CareEventStatus.Closed,
            CareEventActorKind.Staff,
            reason: "结案",
            state);
}
