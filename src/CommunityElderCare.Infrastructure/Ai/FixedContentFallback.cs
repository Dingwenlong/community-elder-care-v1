namespace CommunityElderCare.Infrastructure.Ai;

public sealed class FixedContentFallback
{
    public const string StandardReply =
        "AI 当前不可用，核心求助功能仍可使用。你可以查看提醒，或点击“我需要帮助”联系社区。";

    public const string EmergencyReply =
        "如果能够操作，请立即呼叫身边的人。系统正在把求助发送给社区；当前不会真实拨打 120。";

    public string For(string rejectionCode, bool emergency = false) =>
        emergency ? EmergencyReply : StandardReply;
}
