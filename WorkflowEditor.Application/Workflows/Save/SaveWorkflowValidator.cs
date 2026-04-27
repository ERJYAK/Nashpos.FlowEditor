using System.Text.RegularExpressions;
using FluentValidation;
using WorkflowEditor.Core.Models;
using WorkflowEditor.Core.Models.Steps;

namespace WorkflowEditor.Application.Workflows.Save;

public sealed partial class SaveWorkflowValidator : AbstractValidator<SaveWorkflowCommand>
{
    [GeneratedRegex(@"^[a-z0-9][a-z0-9-]*$", RegexOptions.Compiled)]
    private static partial Regex NameRegex();

    public SaveWorkflowValidator()
    {
        RuleFor(c => c.Document).NotNull();

        When(c => c.Document is not null, () =>
        {
            RuleFor(c => c.Document.Name)
                .NotEmpty()
                .MaximumLength(64)
                .Matches(NameRegex())
                .WithMessage("name must be lowercase letters, digits and hyphens only (e.g. 'import-prices')");

            RuleFor(c => c.Document.Description).MaximumLength(500);

            RuleForEach(c => c.Document.Steps).ChildRules(step =>
            {
                step.RuleFor(s => s).Must(BeValidStep)
                    .WithMessage("step must be either BaseStep with non-empty StepKind or SubflowStep with non-empty SubflowName");
            });
        });
    }

    private static bool BeValidStep(WorkflowStep step) => step switch
    {
        BaseStep b => !string.IsNullOrWhiteSpace(b.StepKind),
        SubflowStep s => !string.IsNullOrWhiteSpace(s.SubflowName),
        _ => false
    };
}
