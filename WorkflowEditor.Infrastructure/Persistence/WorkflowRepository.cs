using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WorkflowEditor.Application.Abstractions;
using WorkflowEditor.Core.Models;
using WorkflowEditor.Core.Serialization;

namespace WorkflowEditor.Infrastructure.Persistence;

internal sealed class WorkflowRepository(AppDbContext db, TimeProvider clock) : IWorkflowRepository
{
    private static readonly JsonSerializerOptions JsonOptions = JsonConfiguration.GetOptions();

    public async Task<WorkflowDocument?> GetAsync(string workflowId, CancellationToken ct)
    {
        var entity = await db.Workflows.AsNoTracking().FirstOrDefaultAsync(w => w.WorkflowId == workflowId, ct);
        return entity is null ? null : Deserialize(entity);
    }

    public async Task<IReadOnlyList<WorkflowSummary>> ListAsync(CancellationToken ct)
    {
        return await db.Workflows
            .AsNoTracking()
            .OrderBy(w => w.Name)
            .Select(w => new WorkflowSummary(w.WorkflowId, w.Name, w.CreatedAt, w.UpdatedAt))
            .ToListAsync(ct);
    }

    public async Task UpsertAsync(WorkflowDocument document, CancellationToken ct)
    {
        var existing = await db.Workflows.FirstOrDefaultAsync(w => w.WorkflowId == document.WorkflowId, ct);
        var now = clock.GetUtcNow().UtcDateTime;
        var payload = JsonSerializer.Serialize(document, JsonOptions);

        if (existing is null)
        {
            db.Workflows.Add(new WorkflowEntity
            {
                WorkflowId = document.WorkflowId,
                Name = document.Name,
                PayloadJson = payload,
                CreatedAt = document.CreatedAt == default ? now : document.CreatedAt,
                UpdatedAt = now,
                Version = 1
            });
        }
        else
        {
            existing.Name = document.Name;
            existing.PayloadJson = payload;
            existing.UpdatedAt = now;
            existing.Version += 1;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(string workflowId, CancellationToken ct)
    {
        var entity = await db.Workflows.FirstOrDefaultAsync(w => w.WorkflowId == workflowId, ct);
        if (entity is null) return false;

        db.Workflows.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static WorkflowDocument Deserialize(WorkflowEntity entity)
    {
        var doc = JsonSerializer.Deserialize<WorkflowDocument>(entity.PayloadJson, JsonOptions);
        return doc ?? throw new InvalidOperationException(
            $"failed to deserialize workflow '{entity.WorkflowId}'");
    }
}
