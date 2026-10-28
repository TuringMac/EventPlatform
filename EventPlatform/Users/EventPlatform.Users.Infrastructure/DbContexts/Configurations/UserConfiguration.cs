using EventPlatform.Users.Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace EventPlatform.Users.Infrastructure.DbContexts.Configurations;

internal class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", "public");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Login)
            .IsRequired()
            .HasMaxLength(400);

        builder.Property(e => e.PasswordHash)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Role)
            .IsRequired();

        builder.HasIndex(e => e.Login)
            .IsUnique();
    }
}
