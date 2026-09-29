using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Sharpify.Core.Data.Interceptors;

namespace Sharpify.Core.Data;

public class SharpifyDesignTimeDbContextFactory : IDesignTimeDbContextFactory<SharpifyDbContext>
{
    public SharpifyDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SharpifyDbContext>();
        optionsBuilder.UseSqlite(
            "Data Source=sharpify.db;Cache=Shared",
            b => b.MigrationsAssembly(typeof(SharpifyDbContext).Assembly.FullName)
        );
        optionsBuilder.AddInterceptors(new SqlitePragmaConnectionInterceptor());

        return new SharpifyDbContext(optionsBuilder.Options);
    }
}
