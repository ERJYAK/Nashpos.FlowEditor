namespace WorkflowEditor.Core.Models;

public record ClipboardData(
    string SerializedNodes,
    string SerializedLinks,
    CanvasPosition BoundingBoxTopLeft
);