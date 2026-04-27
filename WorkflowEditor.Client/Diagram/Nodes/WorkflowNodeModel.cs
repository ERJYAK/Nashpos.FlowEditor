using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using WorkflowEditor.Core.Models;

namespace WorkflowEditor.Client.Diagram.Nodes;

// Узел диаграммы, привязанный к шагу из EditorState по `StepId`.
// Позиция приходит снаружи (из `EditorDocument.NodePositions`), так как сами шаги
// её не хранят — это UI-only слой.
public abstract class WorkflowNodeModel : NodeModel
{
    public string StepId { get; }
    public CanvasPosition LastSyncedPosition { get; private set; }

    protected WorkflowNodeModel(WorkflowStep step, CanvasPosition position)
        : base(new Point(position.X, position.Y))
    {
        StepId = step.Id;
        Title = step.Description;
        LastSyncedPosition = position;
    }

    public void MarkSynced(CanvasPosition position)
    {
        LastSyncedPosition = position;
    }
}
