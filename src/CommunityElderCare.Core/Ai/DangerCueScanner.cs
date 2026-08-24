namespace CommunityElderCare.Core.Ai;

public sealed record DangerCueResult(
    bool IsEmergency,
    bool NeedsConfirmation,
    string Code)
{
    public static DangerCueResult Emergency(string code) => new(true, true, code);
    public static DangerCueResult Confirmation(string code) => new(false, true, code);
    public static DangerCueResult Neutral() => new(false, false, "NONE");
}

public static class DangerCueScanner
{
    public static DangerCueResult Scan(string? input)
    {
        var text = input?.Trim() ?? string.Empty;
        if (text.Length == 0)
        {
            return DangerCueResult.Neutral();
        }

        if (ContainsAny(text, "不想活了", "想死", "自杀", "结束生命"))
        {
            return DangerCueResult.Emergency("SELF_HARM");
        }
        if (ContainsAny(text, "喘不上气", "呼吸困难", "无法呼吸", "不能呼吸"))
        {
            return DangerCueResult.Emergency("BREATHING_DIFFICULTY");
        }
        if (ContainsAny(text, "胸口很痛", "胸口剧痛", "胸痛"))
        {
            return DangerCueResult.Emergency("CHEST_PAIN");
        }
        if (ContainsAny(text, "摔倒", "跌倒") &&
            ContainsAny(text, "起不来", "站不起来", "无法起身"))
        {
            return DangerCueResult.Emergency("FALL_CANNOT_STAND");
        }
        if (ContainsAny(text, "差点摔倒", "差点跌倒", "防滑垫"))
        {
            return DangerCueResult.Confirmation("POSSIBLE_FALL_RISK");
        }
        if (ContainsAny(text, "胸闷", "胸口不舒服"))
        {
            return DangerCueResult.Confirmation("POSSIBLE_CHEST_DISCOMFORT");
        }

        return DangerCueResult.Neutral();
    }

    private static bool ContainsAny(string input, params string[] phrases) =>
        phrases.Any(input.Contains);
}
