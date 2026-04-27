using System.Collections.Immutable;
using WorkflowEditor.Client.Services.Topology;
using WorkflowEditor.Client.Store.Editor;
using WorkflowEditor.Core.Models;
using WorkflowEditor.Tests.Client.TestKit;

namespace WorkflowEditor.Tests.Client.Topology;

public class WorkflowGraphValidatorTests
{
    private static EditorLink Link(string source, string target, string? id = null) =>
        new() { Id = id ?? Guid.NewGuid().ToString(), SourceStepId = source, TargetStepId = target };

    private static ImmutableDictionary<string, EditorLink> Links(params EditorLink[] links) =>
        links.ToImmutableDictionary(l => l.Id);

    [Fact]
    public void Empty_steps_is_invalid()
    {
        var result = WorkflowGraphValidator.ValidateForExport([], Links());
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Single_step_is_valid()
    {
        var result = WorkflowGraphValidator.ValidateForExport([EditorTestData.Base("k", id: "1")], Links());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Linear_chain_is_valid()
    {
        var a = EditorTestData.Base(id: "a");
        var b = EditorTestData.Base(id: "b");
        var c = EditorTestData.Base(id: "c");

        var result = WorkflowGraphValidator.ValidateForExport([a, b, c], Links(Link("a", "b"), Link("b", "c")));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Two_starts_marked_invalid()
    {
        // a, b, c — нет связей внутри (a и b — кандидаты на start)
        var a = EditorTestData.Base(id: "a");
        var b = EditorTestData.Base(id: "b");
        var c = EditorTestData.Base(id: "c");

        // Связь a→c, b — отдельный, оторванный (in=0 out=0).
        var result = WorkflowGraphValidator.ValidateForExport([a, b, c], Links(Link("a", "c")));

        result.IsValid.Should().BeFalse();
        result.InvalidStepIds.Should().Contain("a").And.Contain("b"); // оба с in=0
    }

    [Fact]
    public void Step_with_two_incoming_links_marked_invalid()
    {
        var a = EditorTestData.Base(id: "a");
        var b = EditorTestData.Base(id: "b");
        var c = EditorTestData.Base(id: "c");

        var result = WorkflowGraphValidator.ValidateForExport([a, b, c], Links(Link("a", "c"), Link("b", "c")));

        result.IsValid.Should().BeFalse();
        result.InvalidStepIds.Should().Contain("c");
    }

    [Fact]
    public void Step_with_two_outgoing_links_marked_invalid()
    {
        var a = EditorTestData.Base(id: "a");
        var b = EditorTestData.Base(id: "b");
        var c = EditorTestData.Base(id: "c");

        var result = WorkflowGraphValidator.ValidateForExport([a, b, c], Links(Link("a", "b"), Link("a", "c")));

        result.IsValid.Should().BeFalse();
        result.InvalidStepIds.Should().Contain("a");
    }

    [Fact]
    public void Disconnected_steps_marked_invalid()
    {
        var a = EditorTestData.Base(id: "a");
        var b = EditorTestData.Base(id: "b");

        var result = WorkflowGraphValidator.ValidateForExport([a, b], Links()); // нет связей

        result.IsValid.Should().BeFalse();
    }
}
