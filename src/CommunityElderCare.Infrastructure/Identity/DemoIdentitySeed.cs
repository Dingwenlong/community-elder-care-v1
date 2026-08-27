using CommunityElderCare.Core.Identity;

namespace CommunityElderCare.Infrastructure.Identity;

public static class DemoIdentitySeed
{
    public static readonly Guid ElderUserId = Guid.Parse("11111111-1111-1111-1111-111111111101");
    public static readonly Guid FamilyUserId = Guid.Parse("11111111-1111-1111-1111-111111111102");
    public static readonly Guid CommunityUserId = Guid.Parse("11111111-1111-1111-1111-111111111103");
    public static readonly Guid ServiceWorkerUserId = Guid.Parse("11111111-1111-1111-1111-111111111104");
    public static readonly Guid AdministratorUserId = Guid.Parse("11111111-1111-1111-1111-111111111105");
    public static readonly Guid SecondCommunityUserId = Guid.Parse("11111111-1111-1111-1111-111111111106");
    public static readonly Guid SecondServiceWorkerUserId = Guid.Parse("11111111-1111-1111-1111-111111111107");
    public static readonly Guid MainCareTaskId = Guid.Parse("22222222-2222-2222-2222-222222222201");

    public static IReadOnlyList<UserAccount> BuildAccounts(Guid mainElderId)
    {
        UserAccount[] accounts =
    [
        new(ElderUserId, "elder.demo", DemoRole.Elder, mainElderId, null, null),
        new(FamilyUserId, "family.demo", DemoRole.Family, mainElderId, null, null),
        new(CommunityUserId, "community.demo", DemoRole.CommunityStaff, null, "A01", MainCareTaskId),
        new(ServiceWorkerUserId, "service.demo", DemoRole.ServiceWorker, mainElderId, "A01", MainCareTaskId),
        new(SecondCommunityUserId, "community.second", DemoRole.CommunityStaff, null, "A01", null),
        new(SecondServiceWorkerUserId, "service.second", DemoRole.ServiceWorker, null, "A01", null),
        new(AdministratorUserId, "admin.demo", DemoRole.Administrator, null, null, null),
    ];
        foreach (var account in accounts)
            account.InitializeOperationsProfile(account.Username switch
            {
                "community.demo" => "周敏", "community.second" => "陈佳",
                "service.demo" => "王芳", "service.second" => "刘志远",
                _ => account.Username,
            }, account.AreaCode);
        return accounts;
    }

    public static Guid GetUserId(DemoRole role) => role switch
    {
        DemoRole.Elder => ElderUserId,
        DemoRole.Family => FamilyUserId,
        DemoRole.CommunityStaff => CommunityUserId,
        DemoRole.ServiceWorker => ServiceWorkerUserId,
        DemoRole.Administrator => AdministratorUserId,
        _ => throw new ArgumentOutOfRangeException(nameof(role)),
    };

    public static string GetUsername(DemoRole role) => role switch
    {
        DemoRole.Elder => "elder.demo",
        DemoRole.Family => "family.demo",
        DemoRole.CommunityStaff => "community.demo",
        DemoRole.ServiceWorker => "service.demo",
        DemoRole.Administrator => "admin.demo",
        _ => throw new ArgumentOutOfRangeException(nameof(role)),
    };
}
