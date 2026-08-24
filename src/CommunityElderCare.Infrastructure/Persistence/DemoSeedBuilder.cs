using System.Security.Cryptography;
using System.Text;
using CommunityElderCare.Core.Elders;

namespace CommunityElderCare.Infrastructure.Persistence;

public static class DemoSeedBuilder
{
    private static readonly string[] DemoNames =
    [
        "演示·李安康", "演示·王秋桂", "演示·张守仁", "演示·陈春兰", "演示·刘福生",
        "演示·赵瑞芳", "演示·周长青", "演示·吴月琴", "演示·徐德明", "演示·孙秀华",
        "演示·胡景和", "演示·朱玉珍", "演示·高松年", "演示·林慧芳", "演示·何永泰",
        "演示·郭素芬", "演示·马振华", "演示·罗金梅", "演示·梁泰和", "演示·宋雅琴",
        "演示·郑康宁", "演示·谢芳华", "演示·韩敬民", "演示·唐桂英", "演示·冯寿安",
    ];

    private static readonly (string Code, string Label)[] Risks =
    [
        ("FALL_ATTENTION", "跌倒风险关注"),
        ("BLOOD_PRESSURE_FOLLOWUP", "血压随访关注"),
        ("SLEEP_ATTENTION", "睡眠情况关注"),
        ("MOBILITY_ATTENTION", "行动不便关注"),
    ];

    private static readonly (string Code, string Label)[] Needs =
    [
        ("MEAL", "助餐服务"),
        ("SHOPPING", "生活代购"),
        ("APPOINTMENT_COMPANION", "陪同就诊"),
        ("HOME_VISIT", "上门探访"),
    ];

    public static DemoSeedBatch Build(int count, int seed, DateTimeOffset baseTime)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 15);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, 25);

        var random = new Random(seed);
        var elders = new List<ElderProfile>(count);

        for (var index = 0; index < count; index++)
        {
            var elderId = StableGuid(seed, index, "elder");
            var attentionLevel = index % 5 == 0
                ? CareAttentionLevel.High
                : index % 3 == 0
                    ? CareAttentionLevel.Priority
                    : CareAttentionLevel.Routine;
            var checkInDueAt = index == 0
                ? baseTime.AddMinutes(-30)
                : baseTime.AddHours((index % 6) + 1);
            var profile = new ElderProfile(
                elderId,
                DemoNames[index],
                new DateOnly(random.Next(1938, 1958), random.Next(1, 13), random.Next(1, 25)),
                $"A{(index % 3) + 1:00}",
                attentionLevel,
                checkInDueAt);

            var risk = Risks[index % Risks.Length];
            profile.AddHealthRisk(new HealthRisk(
                StableGuid(seed, index, $"risk-{risk.Code}"), elderId, risk.Code, risk.Label));
            if (attentionLevel == CareAttentionLevel.High)
            {
                var secondRisk = Risks[(index + 1) % Risks.Length];
                profile.AddHealthRisk(new HealthRisk(
                    StableGuid(seed, index, $"risk-{secondRisk.Code}"), elderId, secondRisk.Code, secondRisk.Label));
            }

            var need = Needs[index % Needs.Length];
            profile.AddServiceNeed(new ServiceNeed(
                StableGuid(seed, index, $"need-{need.Code}"), elderId, need.Code, need.Label));
            profile.AddEmergencyContact(new EmergencyContact(
                StableGuid(seed, index, "contact-1"),
                elderId,
                $"演示联系人{index + 1:00}",
                index % 2 == 0 ? "子女" : "亲属",
                $"1990000{index + 1:0000}",
                1));

            elders.Add(profile);
        }

        return new DemoSeedBatch(elders, elders[0].Id, baseTime);
    }

    private static Guid StableGuid(int seed, int index, string kind)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{seed}:{index}:{kind}"));
        return new Guid(bytes.AsSpan(0, 16));
    }
}

public sealed record DemoSeedBatch(
    IReadOnlyList<ElderProfile> Elders,
    Guid MainElderId,
    DateTimeOffset BaseTime);
