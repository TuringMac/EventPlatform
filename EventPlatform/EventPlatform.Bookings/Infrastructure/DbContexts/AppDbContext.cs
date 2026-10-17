using EventPlatform.Bookings.Domain.Model;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace EventPlatform.Bookings.Infrastructure.DbContexts;

public class AppDbContext : DbContext
{
    public DbSet<Booking> Bookings { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
