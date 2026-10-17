using EventPlatform.Users.Domain.Model;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace EventPlatform.Users.Infrastructure.DbContexts;

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
