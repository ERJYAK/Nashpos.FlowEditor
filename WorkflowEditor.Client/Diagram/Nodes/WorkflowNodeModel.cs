namespace WorkflowEditor.Client.Diagram.Nodes;

using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using WorkflowEditor.Core.Models;

// Этот класс живет только на клиенте. Он хранит ID шага из Fluxor.
public abstract class WorkflowNodeModel : NodeModel
{
    public string StepId { get; }

    protected WorkflowNodeModel(WorkflowStep step) 
        : base(new Point(step.Position.X, step.Position.Y))
    {
        StepId = step.Id;
        Title = step.Name;
        // Здесь же будем настраивать порты (входы/выходы)
    }
}