using Microsoft.EntityFrameworkCore;

namespace WorkflowEditor.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    internal DbSet<WorkflowEntity> Workflows => Set<WorkflowEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var workflow = modelBuilder.Entity<WorkflowEntity>();
        workflow.HasKey(w => w.WorkflowId);
        workflow.Property(w => w.WorkflowId).HasMaxLength(64);
        workflow.Property(w => w.Name).HasMaxLength(200);
        workflow.Property(w => w.PayloadJson).IsRequired();
        workflow.Property(w => w.Version).IsConcurrencyToken();
    }
}
