using WorkflowEditor.Core.Models;

namespace WorkflowEditor.Application.Abstractions;

public interface IWorkflowRepository
{
    Task<WorkflowDocument?> GetAsync(string name, CancellationToken ct);
    Task<IReadOnlyList<WorkflowSummary>> ListAsync(CancellationToken ct);
    Task UpsertAsync(WorkflowDocument document, CancellationToken ct);
    Task<bool> DeleteAsync(string name, CancellationToken ct);
}

public sealed record WorkflowSummary(string Name, string Description, DateTime UpdatedAt);
