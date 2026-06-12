using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace PRM.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PrmDbContext>
{
    public PrmDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "PRM.Api"))
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        var optionsBuilder = new DbContextOptionsBuilder<PrmDbContext>();
        optionsBuilder.UseSqlServer(connectionString);
        return new PrmDbContext(optionsBuilder.Options);
    }
}
