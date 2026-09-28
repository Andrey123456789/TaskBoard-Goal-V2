using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Infrastructure.Persistence.Configurations;

internal sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");

        builder.HasKey(x => x.Id);

        // Project names must be unique. Unlike email, the specification does not make this
        // comparison case-insensitive, so names are compared exactly.
        builder.Property(x => x.Name)
            .HasMaxLength(Project.NameMaxLength)
            .UseCollation(Collations.CaseSensitive)
            .IsRequired();

        builder.HasIndex(x => x.Name).IsUnique();

        builder.Property(x => x.Description)
            .HasMaxLength(Project.DescriptionMaxLength);
    }
}
