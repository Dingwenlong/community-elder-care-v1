using CommunityElderCare.Api.Contracts.Auth;
using CommunityElderCare.Core.Identity;
using CommunityElderCare.Infrastructure.Identity;
using CommunityElderCare.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CommunityElderCare.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/auth/login", LoginAsync).AllowAnonymous();
        return endpoints;
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        CommunityCareDbContext dbContext,
        IPasswordHasher<UserAccount> passwordHasher,
        JwtTokenService tokenService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrEmpty(request.Password))
        {
            return InvalidCredentials();
        }

        var account = await dbContext.UserAccounts
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.Username == request.Username.Trim(),
                cancellationToken);
        if (account is null ||
            passwordHasher.VerifyHashedPassword(account, account.PasswordHash, request.Password) ==
            PasswordVerificationResult.Failed)
        {
            return InvalidCredentials();
        }

        var token = tokenService.Create(account);
        return Results.Ok(new LoginResponse(
            token.AccessToken,
            token.ExpiresAt,
            account.Role,
            ShellFor(account.Role),
            IsDemoMode: true));
    }

    private static IResult InvalidCredentials() => Results.Problem(
        statusCode: StatusCodes.Status401Unauthorized,
        title: "Invalid demo credentials",
        extensions: new Dictionary<string, object?> { ["code"] = "INVALID_CREDENTIALS" });

    private static string ShellFor(DemoRole role) => role switch
    {
        DemoRole.Elder => "mobile-elder",
        DemoRole.Family => "mobile-family",
        DemoRole.CommunityStaff or DemoRole.ServiceWorker or DemoRole.Administrator => "admin-web",
        _ => throw new ArgumentOutOfRangeException(nameof(role)),
    };
}
