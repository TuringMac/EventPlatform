using EventPlatform.Bookings.Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventPlatform.Bookings.Infrastructure.DbContexts.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings", "public");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id)
            .ValueGeneratedNever();

        builder.Property(b => b.EventId)
            .IsRequired();

        builder.Property(b => b.UserId)
            .IsRequired();

        builder.Property(b => b.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(b => b.CreatedAt)
            .ValueGeneratedNever();

        builder.Property(b => b.ProcessedAt);

        builder.HasIndex(b => b.Id)
            .IsUnique();
    }
}