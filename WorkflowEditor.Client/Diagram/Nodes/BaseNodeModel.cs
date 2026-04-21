using Blazor.Diagrams.Core.Models;
using WorkflowEditor.Core.Models.Steps;

namespace WorkflowEditor.Client.Diagram.Nodes;

public class BaseNodeModel : WorkflowNodeModel
{
    public BaseNodeModel(BaseStep step) : base(step)
    {
        // КРИТИЧЕСКИ ВАЖНО: Регистрируем якоря для линков
        AddPort(PortAlignment.Top);
        AddPort(PortAlignment.Bottom);
        AddPort(PortAlignment.Left);
        AddPort(PortAlignment.Right);
    }
}