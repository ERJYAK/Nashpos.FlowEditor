using WorkflowEditor.Core.Models;

namespace WorkflowEditor.Application.Abstractions;

public interface IWorkflowRepository
{
    Task<WorkflowDocument?> GetAsync(string workflowId, CancellationToken ct);
    Task<IReadOnlyList<WorkflowSummary>> ListAsync(CancellationToken ct);
    Task UpsertAsync(WorkflowDocument document, CancellationToken ct);
    Task<bool> DeleteAsync(string workflowId, CancellationToken ct);
}

public sealed record WorkflowSummary(string WorkflowId, string Name, DateTime CreatedAt, DateTime UpdatedAt);
