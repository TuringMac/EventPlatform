using EventPlatform.Events.Domain.Model;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace EventPlatform.Events.Infrastructure.DbContexts;

public class AppDbContext : DbContext
{
    public DbSet<Event> Events { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
