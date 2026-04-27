using WorkflowEditor.Application.Abstractions;
using WorkflowEditor.Application.Common;

namespace WorkflowEditor.Application.Workflows.List;

public sealed record ListWorkflowsQuery;

public interface IListWorkflowsQueryHandler
{
    Task<Result<IReadOnlyList<WorkflowSummary>>> HandleAsync(ListWorkflowsQuery query, CancellationToken ct);
}

public sealed class ListWorkflowsQueryHandler(IWorkflowRepository repository) : IListWorkflowsQueryHandler
{
    public async Task<Result<IReadOnlyList<WorkflowSummary>>> HandleAsync(ListWorkflowsQuery query, CancellationToken ct)
    {
        var summaries = await repository.ListAsync(ct);
        return Result<IReadOnlyList<WorkflowSummary>>.Success(summaries);
    }
}
