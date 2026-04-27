using WorkflowEditor.Application.Abstractions;
using WorkflowEditor.Application.Common;

namespace WorkflowEditor.Application.Workflows.Delete;

public sealed record DeleteWorkflowCommand(string WorkflowId);

public interface IDeleteWorkflowCommandHandler
{
    Task<Result<bool>> HandleAsync(DeleteWorkflowCommand command, CancellationToken ct);
}

public sealed class DeleteWorkflowCommandHandler(IWorkflowRepository repository) : IDeleteWorkflowCommandHandler
{
    public async Task<Result<bool>> HandleAsync(DeleteWorkflowCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.WorkflowId))
            return Error.Validation("workflowId is required",
                new Dictionary<string, string[]> { ["workflowId"] = ["required"] });

        var deleted = await repository.DeleteAsync(command.WorkflowId, ct);
        return deleted
            ? Result<bool>.Success(true)
            : Error.NotFound($"workflow '{command.WorkflowId}' not found");
    }
}
