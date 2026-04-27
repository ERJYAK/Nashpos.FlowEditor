using WorkflowEditor.Application.Abstractions;
using WorkflowEditor.Application.Common;
using WorkflowEditor.Core.Models;

namespace WorkflowEditor.Application.Workflows.Get;

public sealed class GetWorkflowQueryHandler(IWorkflowRepository repository) : IGetWorkflowQueryHandler
{
    public async Task<Result<WorkflowDocument>> HandleAsync(GetWorkflowQuery query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query.WorkflowId))
            return Error.Validation("workflowId is required",
                new Dictionary<string, string[]> { ["workflowId"] = ["required"] });

        var document = await repository.GetAsync(query.WorkflowId, ct);
        return document is null
            ? Error.NotFound($"workflow '{query.WorkflowId}' not found")
            : Result<WorkflowDocument>.Success(document);
    }
}
