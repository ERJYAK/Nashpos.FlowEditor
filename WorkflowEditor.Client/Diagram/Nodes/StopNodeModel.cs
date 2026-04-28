using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using WorkflowEditor.Client.Store.Editor;
using WorkflowEditor.Core.Models;

namespace WorkflowEditor.Client.Diagram.Nodes;

// Виртуальный узел-«хвост» для веток BREAK_WORKFLOW / SILENT_BREAK_WORKFLOW.
// Не привязан к WorkflowStep — данные берутся из EditorDocument.VirtualStops по `StopId`.
public sealed class StopNodeModel : NodeModel
{
    public string StopId { get; }
    public string OwnerStepId { get; }
    public EditorLinkKind BranchKind { get; }
    public int? WhenCode { get; }
    public bool IsSilent { get; }
    public int? ErrorCode { get; }
    public string? ErrorMessage { get; }

    public StopNodeModel(StopNodeInfo info, CanvasPosition position)
        : base(new Point(position.X, position.Y))
    {
        StopId = info.Id;
        OwnerStepId = info.OwnerStepId;
        BranchKind = info.BranchKind;
        WhenCode = info.WhenCode;
        IsSilent = info.IsSilent;
        ErrorCode = info.ErrorCode;
        ErrorMessage = info.ErrorMessage;

        AddPort(PortAlignment.Top);
        AddPort(PortAlignment.Left);
    }
}
