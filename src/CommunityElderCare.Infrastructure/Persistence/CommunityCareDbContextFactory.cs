using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CommunityElderCare.Infrastructure.Persistence;

public sealed class CommunityCareDbContextFactory : IDesignTimeDbContextFactory<CommunityCareDbContext>
{
    public CommunityCareDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CommunityCareDbContext>()
            .UseSqlite("Data Source=community-care.db")
            .Options;

        return new CommunityCareDbContext(options);
    }
}
