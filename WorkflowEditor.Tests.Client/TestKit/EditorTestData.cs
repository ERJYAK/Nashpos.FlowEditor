using System.Collections.Immutable;
using WorkflowEditor.Client.Store.Editor;
using WorkflowEditor.Core.Models;
using WorkflowEditor.Core.Models.Steps;

namespace WorkflowEditor.Tests.Client.TestKit;

internal static class EditorTestData
{
    public static BaseStep BaseStep(string id, string name = "task", double x = 0, double y = 0) =>
        new() { Id = id, Name = name, Position = new CanvasPosition(x, y) };

    public static SubflowStep SubflowStep(string id, string name = "subflow", string subflowId = "sub-1",
        double x = 0, double y = 0) =>
        new() { Id = id, Name = name, SubflowId = subflowId, Position = new CanvasPosition(x, y) };

    public static WorkflowLink Link(string id, string sourceStep, string targetStep,
        string sourcePort = "Right", string targetPort = "Left") =>
        new()
        {
            Id = id,
            SourceNodeId = sourceStep,
            SourcePortId = sourcePort,
            TargetNodeId = targetStep,
            TargetPortId = targetPort
        };

    public static WorkflowDocument Document(string id, params WorkflowStep[] steps) =>
        new()
        {
            WorkflowId = id,
            Name = "doc",
            Steps = steps.ToImmutableDictionary(s => s.Id),
            Links = ImmutableDictionary<string, WorkflowLink>.Empty
        };

    public static WorkflowDocument WithLinks(this WorkflowDocument doc, params WorkflowLink[] links) =>
        doc with { Links = links.ToImmutableDictionary(l => l.Id) };

    public static EditorState StateWith(WorkflowDocument document, string? activeId = null) =>
        new EditorState() with
        {
            OpenDocuments = ImmutableDictionary<string, WorkflowDocument>.Empty
                .SetItem(document.WorkflowId, document),
            ActiveDocumentId = activeId ?? document.WorkflowId
        };
}
