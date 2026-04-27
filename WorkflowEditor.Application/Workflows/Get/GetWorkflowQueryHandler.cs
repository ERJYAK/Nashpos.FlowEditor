using WorkflowEditor.Application.Abstractions;
using WorkflowEditor.Application.Common;
using WorkflowEditor.Core.Models;

namespace WorkflowEditor.Application.Workflows.Get;

public sealed class GetWorkflowQueryHandler(IWorkflowRepository repository) : IGetWorkflowQueryHandler
{
    public async Task<Result<WorkflowDocument>> HandleAsync(GetWorkflowQuery query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query.Name))
            return Error.Validation("name is required",
                new Dictionary<string, string[]> { ["name"] = ["required"] });

        var document = await repository.GetAsync(query.Name, ct);
        return document is null
            ? Error.NotFound($"workflow '{query.Name}' not found")
            : Result<WorkflowDocument>.Success(document);
    }
}
