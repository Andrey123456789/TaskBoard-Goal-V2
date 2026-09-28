using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(User.NameMaxLength)
            .IsRequired();

        // Email uniqueness is case-insensitive.
        builder.Property(x => x.Email)
            .HasMaxLength(User.EmailMaxLength)
            .UseCollation(Collations.CaseInsensitive)
            .IsRequired();

        builder.HasIndex(x => x.Email).IsUnique();

        // The system Deleted User is schema reference data, so every migrated database contains
        // it independently of Development demo seeding. It has no email (empty string), which
        // can never collide with the valid email of an ordinary user.
        builder.HasData(new User
        {
            Id = User.DeletedUserId,
            Name = User.DeletedUserName,
            Email = string.Empty,
            CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        });
    }
}
