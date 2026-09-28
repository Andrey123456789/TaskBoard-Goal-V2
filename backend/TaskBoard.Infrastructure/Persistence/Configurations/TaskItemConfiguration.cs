using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Infrastructure.Persistence.Configurations;

internal sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("Tasks");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .HasMaxLength(TaskItem.TitleMaxLength)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(TaskItem.DescriptionMaxLength);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Deleting a project never cascades to tasks; a project with tasks cannot be deleted.
        builder.HasOne(x => x.Project)
            .WithMany()
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        // Deleting a user never deletes tasks; they are reassigned to Deleted User first.
        builder.HasOne(x => x.Assignee)
            .WithMany()
            .HasForeignKey(x => x.AssigneeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.Status);
    }
}
