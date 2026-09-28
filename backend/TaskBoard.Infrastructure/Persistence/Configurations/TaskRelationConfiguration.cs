using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Infrastructure.Persistence.Configurations;

internal sealed class TaskRelationConfiguration : IEntityTypeConfiguration<TaskRelation>
{
    public void Configure(EntityTypeBuilder<TaskRelation> builder)
    {
        builder.ToTable("TaskRelations", table =>
            table.HasCheckConstraint("CK_TaskRelations_NotSelf", "[FirstTaskId] <> [SecondTaskId]"));

        // One row per relationship; TaskRelationKey orders the pair canonically.
        builder.HasKey(x => new { x.FirstTaskId, x.SecondTaskId });

        // SQL Server rejects two cascading paths to Tasks, so relationship rows are removed
        // explicitly together with their task instead.
        builder.HasOne(x => x.FirstTask)
            .WithMany()
            .HasForeignKey(x => x.FirstTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SecondTask)
            .WithMany()
            .HasForeignKey(x => x.SecondTaskId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
