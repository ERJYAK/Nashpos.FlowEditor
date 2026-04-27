using Blazor.Diagrams.Core.Models;
using WorkflowEditor.Core.Models;
using WorkflowEditor.Core.Models.Steps;

namespace WorkflowEditor.Client.Diagram.Nodes;

public sealed class BaseNodeModel : WorkflowNodeModel
{
    public string StepKind { get; }

    public BaseNodeModel(BaseStep step, CanvasPosition position) : base(step, position)
    {
        StepKind = step.StepKind;

        AddPort(PortAlignment.Top);
        AddPort(PortAlignment.Bottom);
        AddPort(PortAlignment.Left);
        AddPort(PortAlignment.Right);
    }
}
