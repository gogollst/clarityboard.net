using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ClarityBoard.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ClarityBoardContext>
{
    public ClarityBoardContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ClarityBoardContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=clarityboard;Username=app;Password=devpassword");

        return new ClarityBoardContext(optionsBuilder.Options);
    }
}
