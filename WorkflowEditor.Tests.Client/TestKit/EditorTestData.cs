using System.Collections.Immutable;
using WorkflowEditor.Core.Models;
using WorkflowEditor.Core.Models.Steps;

namespace WorkflowEditor.Tests.Client.TestKit;

internal static class EditorTestData
{
    public static BaseStep Base(string stepKind = "task", string description = "", string? id = null) =>
        new()
        {
            Id = id ?? Guid.NewGuid().ToString(),
            StepKind = stepKind,
            Description = description
        };

    public static SubflowStep Sub(string subflowName = "sub-1", string description = "", string? id = null) =>
        new()
        {
            Id = id ?? Guid.NewGuid().ToString(),
            SubflowName = subflowName,
            Description = description
        };

    public static WorkflowDocument Document(string name, string description = "", params WorkflowStep[] steps) =>
        new()
        {
            Name = name,
            Description = description,
            Steps = steps.ToImmutableList()
        };
}
