using Blazor.Diagrams.Core.Models;
using WorkflowEditor.Core.Models;
using WorkflowEditor.Core.Models.Steps;

namespace WorkflowEditor.Client.Diagram.Nodes;

// Узел только адресуется по StepId. SubflowName/Description — читаются виджетом из state.
public sealed class SubflowNodeModel : WorkflowNodeModel
{
    public SubflowNodeModel(SubflowStep step, CanvasPosition position) : base(step, position)
    {
        AddPort(PortAlignment.Left);
        AddPort(PortAlignment.Right);
    }
}
