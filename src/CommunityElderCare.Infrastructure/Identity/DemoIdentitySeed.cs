using CommunityElderCare.Core.Identity;

namespace CommunityElderCare.Infrastructure.Identity;

public static class DemoIdentitySeed
{
    public static readonly Guid ElderUserId = Guid.Parse("11111111-1111-1111-1111-111111111101");
    public static readonly Guid FamilyUserId = Guid.Parse("11111111-1111-1111-1111-111111111102");
    public static readonly Guid CommunityUserId = Guid.Parse("11111111-1111-1111-1111-111111111103");
    public static readonly Guid ServiceWorkerUserId = Guid.Parse("11111111-1111-1111-1111-111111111104");
    public static readonly Guid AdministratorUserId = Guid.Parse("11111111-1111-1111-1111-111111111105");
    public static readonly Guid MainCareTaskId = Guid.Parse("22222222-2222-2222-2222-222222222201");

    public static IReadOnlyList<UserAccount> BuildAccounts(Guid mainElderId) =>
    [
        new(ElderUserId, "elder.demo", DemoRole.Elder, mainElderId, null, null),
        new(FamilyUserId, "family.demo", DemoRole.Family, mainElderId, null, null),
        new(CommunityUserId, "community.demo", DemoRole.CommunityStaff, null, "A01", MainCareTaskId),
        new(ServiceWorkerUserId, "service.demo", DemoRole.ServiceWorker, mainElderId, null, MainCareTaskId),
        new(AdministratorUserId, "admin.demo", DemoRole.Administrator, null, null, null),
    ];

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
