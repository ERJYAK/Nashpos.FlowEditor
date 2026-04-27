using Blazor.Diagrams.Core.Models;
using WorkflowEditor.Core.Models;
using WorkflowEditor.Core.Models.Steps;

namespace WorkflowEditor.Client.Diagram.Nodes;

// Узел только адресуется по StepId. Все отображаемые свойства (StepKind, Description)
// читаются виджетом напрямую из EditorState — иначе get-only поля nodeModel остаются
// stale после reducer-update.
public sealed class BaseNodeModel : WorkflowNodeModel
{
    public BaseNodeModel(BaseStep step, CanvasPosition position) : base(step, position)
    {
        AddPort(PortAlignment.Top);
        AddPort(PortAlignment.Bottom);
        AddPort(PortAlignment.Left);
        AddPort(PortAlignment.Right);
    }
}
