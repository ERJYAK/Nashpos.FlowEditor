namespace WorkflowEditor.Core.Models.Steps;

public record BaseStep : WorkflowStep
{
    public override WorkflowStep WithName(string name) => this with { Name = name };

    public override WorkflowStep WithPosition(CanvasPosition position) => this with { Position = position };

    public override WorkflowStep CloneWithId(string newId) => this with { Id = newId };
}
