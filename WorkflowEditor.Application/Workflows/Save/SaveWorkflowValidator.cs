using System.Collections.Immutable;
using FluentValidation;
using WorkflowEditor.Core.Models;

namespace WorkflowEditor.Application.Workflows.Save;

public sealed class SaveWorkflowValidator : AbstractValidator<SaveWorkflowCommand>
{
    public SaveWorkflowValidator()
    {
        RuleFor(c => c.Document).NotNull();

        When(c => c.Document is not null, () =>
        {
            RuleFor(c => c.Document.WorkflowId)
                .NotEmpty()
                .Must(id => Guid.TryParse(id, out _))
                .WithMessage("workflowId must be a valid GUID");

            RuleFor(c => c.Document.Name)
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(c => c.Document.Steps)
                .Must(StepKeysMatchIds)
                .WithMessage("step dictionary key must equal step.Id");

            RuleFor(c => c.Document)
                .Must(LinksReferenceExistingSteps)
                .WithMessage("every link must reference existing source and target steps");
        });
    }

    private static bool StepKeysMatchIds(ImmutableDictionary<string, WorkflowStep> steps)
    {
        foreach (var (key, step) in steps)
        {
            if (!string.Equals(key, step.Id, StringComparison.Ordinal)) return false;
        }
        return true;
    }

    private static bool LinksReferenceExistingSteps(WorkflowDocument doc)
    {
        foreach (var link in doc.Links.Values)
        {
            if (!doc.Steps.ContainsKey(link.SourceNodeId)) return false;
            if (!doc.Steps.ContainsKey(link.TargetNodeId)) return false;
        }
        return true;
    }
}
