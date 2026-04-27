using WorkflowEditor.Application.Abstractions;
using WorkflowEditor.Application.Common;

namespace WorkflowEditor.Application.Workflows.Delete;

public sealed record DeleteWorkflowCommand(string Name);

public interface IDeleteWorkflowCommandHandler
{
    Task<Result<bool>> HandleAsync(DeleteWorkflowCommand command, CancellationToken ct);
}

public sealed class DeleteWorkflowCommandHandler(IWorkflowRepository repository) : IDeleteWorkflowCommandHandler
{
    public async Task<Result<bool>> HandleAsync(DeleteWorkflowCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
            return Error.Validation("name is required",
                new Dictionary<string, string[]> { ["name"] = ["required"] });

        var deleted = await repository.DeleteAsync(command.Name, ct);
        return deleted
            ? Result<bool>.Success(true)
            : Error.NotFound($"workflow '{command.Name}' not found");
    }
}
