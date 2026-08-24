using CommunityElderCare.Core.Ai;

namespace CommunityElderCare.UnitTests.Ai;

public sealed class DangerCueScannerTests
{
    [Theory]
    [InlineData("我摔倒了，起不来", "FALL_CANNOT_STAND")]
    [InlineData("胸口很痛", "CHEST_PAIN")]
    [InlineData("我喘不上气", "BREATHING_DIFFICULTY")]
    [InlineData("我不想活了", "SELF_HARM")]
    public void Explicit_danger_cues_bypass_the_model(string input, string code)
    {
        var result = DangerCueScanner.Scan(input);

        Assert.True(result.IsEmergency);
        Assert.Equal(code, result.Code);
    }

    [Theory]
    [InlineData("昨天差点摔倒，想看看防滑垫", "POSSIBLE_FALL_RISK")]
    [InlineData("最近有点胸闷，但现在没事", "POSSIBLE_CHEST_DISCOMFORT")]
    public void Ambiguous_or_historical_cues_require_confirmation(string input, string code)
    {
        var result = DangerCueScanner.Scan(input);

        Assert.False(result.IsEmergency);
        Assert.True(result.NeedsConfirmation);
        Assert.Equal(code, result.Code);
    }

    [Fact]
    public void Ordinary_companionship_question_is_neutral()
    {
        var result = DangerCueScanner.Scan("社区活动几点开始？");

        Assert.False(result.IsEmergency);
        Assert.False(result.NeedsConfirmation);
        Assert.Equal("NONE", result.Code);
    }
}
