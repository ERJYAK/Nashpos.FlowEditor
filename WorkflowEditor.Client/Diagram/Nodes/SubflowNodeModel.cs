using Blazor.Diagrams.Core.Models;
using WorkflowEditor.Core.Models;
using WorkflowEditor.Core.Models.Steps;

namespace WorkflowEditor.Client.Diagram.Nodes;

public sealed class SubflowNodeModel : WorkflowNodeModel
{
    public string SubflowName { get; }

    public SubflowNodeModel(SubflowStep step, CanvasPosition position) : base(step, position)
    {
        SubflowName = step.SubflowName;
        AddPort(PortAlignment.Left);
        AddPort(PortAlignment.Right);
    }
}
