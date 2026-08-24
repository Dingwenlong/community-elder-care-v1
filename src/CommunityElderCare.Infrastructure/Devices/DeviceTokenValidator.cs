using System.Security.Cryptography;
using System.Text;
using CommunityElderCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityElderCare.Infrastructure.Devices;

public sealed class DeviceTokenValidator(CommunityCareDbContext dbContext)
{
    public async Task<bool> ValidateAsync(
        Guid deviceId,
        string? rawToken,
        CancellationToken cancellationToken)
    {
        if (deviceId == Guid.Empty || string.IsNullOrWhiteSpace(rawToken))
        {
            return false;
        }

        var storedHash = await dbContext.Devices
            .AsNoTracking()
            .Where(device => device.Id == deviceId && device.IsEnabled)
            .Select(device => device.TokenHash)
            .SingleOrDefaultAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(storedHash))
        {
            return false;
        }

        var suppliedHash = HashToken(rawToken);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(storedHash),
            Encoding.ASCII.GetBytes(suppliedHash));
    }

    public static string HashToken(string rawToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawToken);
        return Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)))
            .ToLowerInvariant();
    }
}
