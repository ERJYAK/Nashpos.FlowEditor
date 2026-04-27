using FluentValidation;
using WorkflowEditor.Application.Abstractions;
using WorkflowEditor.Application.Common;
using WorkflowEditor.Core.Models;

namespace WorkflowEditor.Application.Workflows.Save;

public sealed class SaveWorkflowCommandHandler(
    IWorkflowRepository repository,
    IValidator<SaveWorkflowCommand> validator) : ISaveWorkflowCommandHandler
{
    public async Task<Result<WorkflowDocument>> HandleAsync(SaveWorkflowCommand command, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            var failures = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            return Error.Validation("workflow document is invalid", failures);
        }

        await repository.UpsertAsync(command.Document, ct);
        return Result<WorkflowDocument>.Success(command.Document);
    }
}
