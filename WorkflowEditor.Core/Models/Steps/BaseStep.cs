namespace WorkflowEditor.Core.Models.Steps;

// Базовый шаг — соответствует `{ "step": "<kind>" }` в JSON-формате.
// `StepKind` — имя зарегистрированного на стороне исполнителя обработчика
// (например, `download-package`, `transform-xml`).
public sealed record BaseStep : WorkflowStep
{
    public string StepKind { get; init; } = string.Empty;

    public BaseStep WithStepKind(string stepKind) => this with { StepKind = stepKind };

    public override WorkflowStep WithDescription(string description) =>
        this with { Description = description };

    public override WorkflowStep CloneAsNew() =>
        this with { Id = Guid.NewGuid().ToString() };
}
