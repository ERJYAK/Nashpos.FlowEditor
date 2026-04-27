using WorkflowEditor.Application.Common;
using WorkflowEditor.Core.Models;

namespace WorkflowEditor.Application.Workflows.Save;

public sealed record SaveWorkflowCommand(WorkflowDocument Document);

public interface ISaveWorkflowCommandHandler
{
    Task<Result<WorkflowDocument>> HandleAsync(SaveWorkflowCommand command, CancellationToken ct);
}
