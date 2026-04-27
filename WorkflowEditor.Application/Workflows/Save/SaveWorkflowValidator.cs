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
                .Must(HaveUniqueIds)
                .WithMessage("step ids must be unique");

            RuleFor(c => c.Document)
                .Must(LinksReferenceExistingSteps)
                .WithMessage("every link must reference existing source and target steps");
        });
    }

    private static bool HaveUniqueIds(IReadOnlyList<WorkflowStep> steps)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var step in steps)
        {
            if (!seen.Add(step.Id)) return false;
        }
        return true;
    }

    private static bool LinksReferenceExistingSteps(WorkflowDocument doc)
    {
        var stepIds = doc.Steps.Select(s => s.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var link in doc.Links)
        {
            if (!stepIds.Contains(link.SourceNodeId)) return false;
            if (!stepIds.Contains(link.TargetNodeId)) return false;
        }
        return true;
    }
}
