using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using WorkflowEditor.Core.Models;

namespace WorkflowEditor.Client.Diagram.Nodes;

// Этот класс живет только на клиенте. Он хранит ID шага из Fluxor.
public abstract class WorkflowNodeModel : NodeModel
{
    public string StepId { get; }

    // Последняя позиция, в которой узел был синхронизирован из state-store.
    // Используется, чтобы отличить «не двигался» от «передвинут пользователем»
    // — без хардкода (0,0) и без обращения к state.Steps по StepId.
    public CanvasPosition LastSyncedPosition { get; private set; }

    protected WorkflowNodeModel(WorkflowStep step)
        : base(new Point(step.Position.X, step.Position.Y))
    {
        StepId = step.Id;
        Title = step.Name;
        LastSyncedPosition = step.Position;
    }

    public void MarkSynced(CanvasPosition position)
    {
        LastSyncedPosition = position;
    }
}
