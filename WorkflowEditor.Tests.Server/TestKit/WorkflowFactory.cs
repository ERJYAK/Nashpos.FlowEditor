using System.Collections.Immutable;
using WorkflowEditor.Core.Models;
using WorkflowEditor.Core.Models.Steps;

namespace WorkflowEditor.Tests.Server.TestKit;

internal static class WorkflowFactory
{
    public static WorkflowDocument Document(string id, params WorkflowStep[] steps) => new()
    {
        WorkflowId = id,
        Name = "test workflow",
        Steps = steps.ToImmutableDictionary(s => s.Id),
        Links = ImmutableDictionary<string, WorkflowLink>.Empty
    };

    public static BaseStep BaseStep(string id) => new() { Id = id, Name = "task" };

    public static WorkflowDocument WithLinks(this WorkflowDocument doc, params WorkflowLink[] links) =>
        doc with { Links = links.ToImmutableDictionary(l => l.Id) };

    public static WorkflowLink Link(string id, string source, string target) => new()
    {
        Id = id,
        SourceNodeId = source,
        TargetNodeId = target,
        SourcePortId = "Right",
        TargetPortId = "Left"
    };
}
