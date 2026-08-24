using Microsoft.EntityFrameworkCore;

namespace CommunityElderCare.Infrastructure.Persistence;

public sealed class CommunityCareDbContext(DbContextOptions<CommunityCareDbContext> options)
    : DbContext(options);
