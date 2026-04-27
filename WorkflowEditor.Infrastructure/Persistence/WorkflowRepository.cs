using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WorkflowEditor.Application.Abstractions;
using WorkflowEditor.Core.Models;
using WorkflowEditor.Core.Serialization;

namespace WorkflowEditor.Infrastructure.Persistence;

internal sealed class WorkflowRepository(AppDbContext db, TimeProvider clock) : IWorkflowRepository
{
    private static readonly JsonSerializerOptions JsonOptions = JsonConfiguration.GetOptions();

    public async Task<WorkflowDocument?> GetAsync(string name, CancellationToken ct)
    {
        var entity = await db.Workflows.AsNoTracking().FirstOrDefaultAsync(w => w.Name == name, ct);
        return entity is null ? null : Deserialize(entity);
    }

    public async Task<IReadOnlyList<WorkflowSummary>> ListAsync(CancellationToken ct)
    {
        return await db.Workflows
            .AsNoTracking()
            .OrderBy(w => w.Name)
            .Select(w => new WorkflowSummary(w.Name, w.Description, w.UpdatedAt))
            .ToListAsync(ct);
    }

    public async Task UpsertAsync(WorkflowDocument document, CancellationToken ct)
    {
        var existing = await db.Workflows.FirstOrDefaultAsync(w => w.Name == document.Name, ct);
        var now = clock.GetUtcNow().UtcDateTime;
        var payload = JsonSerializer.Serialize(document, JsonOptions);

        if (existing is null)
        {
            db.Workflows.Add(new WorkflowEntity
            {
                Name = document.Name,
                Description = document.Description,
                PayloadJson = payload,
                UpdatedAt = now,
                Version = 1
            });
        }
        else
        {
            existing.Description = document.Description;
            existing.PayloadJson = payload;
            existing.UpdatedAt = now;
            existing.Version += 1;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(string name, CancellationToken ct)
    {
        var entity = await db.Workflows.FirstOrDefaultAsync(w => w.Name == name, ct);
        if (entity is null) return false;

        db.Workflows.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static WorkflowDocument Deserialize(WorkflowEntity entity)
    {
        var doc = JsonSerializer.Deserialize<WorkflowDocument>(entity.PayloadJson, JsonOptions);
        return doc is null
            ? throw new InvalidOperationException($"failed to deserialize workflow '{entity.Name}'")
            : doc with { Name = entity.Name };
    }
}
