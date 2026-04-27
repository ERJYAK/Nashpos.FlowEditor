using System.Collections.Immutable;
using WorkflowEditor.Client.Services.Topology;
using WorkflowEditor.Client.Store.Editor;
using WorkflowEditor.Core.Models;
using WorkflowEditor.Tests.Client.TestKit;

namespace WorkflowEditor.Tests.Client.Topology;

public class StepOrderResolverTests
{
    private static EditorLink Link(string source, string target, string? id = null) =>
        new() { Id = id ?? Guid.NewGuid().ToString(), SourceStepId = source, TargetStepId = target };

    private static ImmutableDictionary<string, EditorLink> Links(params EditorLink[] links) =>
        links.ToImmutableDictionary(l => l.Id);

    [Fact]
    public void Empty_steps_returns_empty_success()
    {
        var result = StepOrderResolver.Resolve([], Links());
        result.IsSuccess.Should().BeTrue();
        result.Ordered.Should().BeEmpty();
    }

    [Fact]
    public void Single_step_returns_that_step()
    {
        var s = EditorTestData.Base(id: "1");
        var result = StepOrderResolver.Resolve([s], Links());
        result.IsSuccess.Should().BeTrue();
        result.Ordered!.Should().ContainSingle().Which.Id.Should().Be("1");
    }

    [Fact]
    public void Linear_chain_topologically_sorts()
    {
        var a = EditorTestData.Base(id: "a");
        var b = EditorTestData.Base(id: "b");
        var c = EditorTestData.Base(id: "c");

        var result = StepOrderResolver.Resolve(
            [c, a, b], // intentionally out of order
            Links(Link("a", "b"), Link("b", "c")));

        result.IsSuccess.Should().BeTrue();
        result.Ordered!.Select(s => s.Id).Should().ContainInOrder("a", "b", "c");
    }

    [Fact]
    public void Branching_two_outgoing_from_same_node_fails()
    {
        var a = EditorTestData.Base(id: "a");
        var b = EditorTestData.Base(id: "b");
        var c = EditorTestData.Base(id: "c");

        var result = StepOrderResolver.Resolve([a, b, c], Links(Link("a", "b"), Link("a", "c")));

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("исходящ");
    }

    [Fact]
    public void Two_incoming_to_same_node_fails()
    {
        var a = EditorTestData.Base(id: "a");
        var b = EditorTestData.Base(id: "b");
        var c = EditorTestData.Base(id: "c");

        var result = StepOrderResolver.Resolve([a, b, c], Links(Link("a", "c"), Link("b", "c")));

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("входящ");
    }

    [Fact]
    public void Cycle_fails()
    {
        var a = EditorTestData.Base(id: "a");
        var b = EditorTestData.Base(id: "b");

        var result = StepOrderResolver.Resolve([a, b], Links(Link("a", "b"), Link("b", "a")));

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Match(s => s!.Contains("цикл") || s.Contains("несвязанн"));
    }

    [Fact]
    public void Disconnected_chains_fail_with_explicit_message()
    {
        var a = EditorTestData.Base(id: "a");
        var b = EditorTestData.Base(id: "b");
        var c = EditorTestData.Base(id: "c");
        var d = EditorTestData.Base(id: "d");

        var result = StepOrderResolver.Resolve([a, b, c, d], Links(Link("a", "b"), Link("c", "d")));

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("несколько несвязанных цепочек");
    }
}
