using WorkflowEditor.Application.Common;
using WorkflowEditor.Core.Models;

namespace WorkflowEditor.Application.Workflows.Get;

public sealed record GetWorkflowQuery(string Name);

public interface IGetWorkflowQueryHandler
{
    Task<Result<WorkflowDocument>> HandleAsync(GetWorkflowQuery query, CancellationToken ct);
}
