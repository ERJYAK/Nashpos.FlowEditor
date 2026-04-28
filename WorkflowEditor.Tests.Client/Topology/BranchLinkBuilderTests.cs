using System.Collections.Immutable;
using WorkflowEditor.Client.Services.Topology;
using WorkflowEditor.Client.Store.Editor;
using WorkflowEditor.Core.Models;
using WorkflowEditor.Core.Models.Steps;
using WorkflowEditor.Tests.Client.TestKit;

namespace WorkflowEditor.Tests.Client.Topology;

public class BranchLinkBuilderTests
{
    [Fact]
    public void Linear_steps_without_branches_produce_default_links()
    {
        var a = EditorTestData.Base(id: "a");
        var b = EditorTestData.Base(id: "b");
        var c = EditorTestData.Base(id: "c");

        var result = BranchLinkBuilder.Build([a, b, c]);

        result.Links.Should().HaveCount(2);
        result.Links.Values.Should().AllSatisfy(l =>
        {
            l.Kind.Should().Be(EditorLinkKind.Default);
            l.Decision.Should().Be(Decision.NextStep);
        });
        result.VirtualStops.Should().BeEmpty();
    }

    [Fact]
    public void OnSuccess_with_goto_creates_link_to_persistent_id()
    {
        var src = new BaseStep
        {
            Id = "src", StepKind = "k",
            OnSuccess = new Branch { Decision = Decision.GotoStep, StepId = "tgt" }
        };
        var middle = EditorTestData.Base(id: "middle");
        var target = new BaseStep { Id = "real_tgt", StepKind = "k2", StepId = "tgt" };

        var result = BranchLinkBuilder.Build([src, middle, target]);

        var fromSrc = result.Links.Values.Where(l => l.SourceStepId == "src").ToList();
        fromSrc.Should().ContainSingle()
            .Which.Should().Match<EditorLink>(l =>
                l.Kind == EditorLinkKind.OnSuccess
                && l.Decision == Decision.GotoStep
                && l.TargetStepId == "real_tgt");
    }

    [Fact]
    public void Break_workflow_creates_virtual_stop_node()
    {
        var step = new BaseStep
        {
            Id = "guard", StepKind = "k",
            OnSuccess = new Branch
            {
                Decision = Decision.BreakWorkflow,
                ErrorCode = 5000,
                ErrorMessage = "Not allowed"
            }
        };

        var result = BranchLinkBuilder.Build([step]);

        result.VirtualStops.Should().HaveCount(1);
        var stop = result.VirtualStops.Values.Single();
        stop.OwnerStepId.Should().Be("guard");
        stop.IsSilent.Should().BeFalse();
        stop.ErrorCode.Should().Be(5000);
        stop.ErrorMessage.Should().Be("Not allowed");

        var link = result.Links.Values.Single();
        link.TargetStepId.Should().Be(stop.Id);
        link.Decision.Should().Be(Decision.BreakWorkflow);
    }

    [Fact]
    public void Silent_break_creates_silent_stop_node()
    {
        var step = new BaseStep
        {
            Id = "x", StepKind = "k",
            OnSuccess = new Branch { Decision = Decision.SilentBreakWorkflow }
        };

        var result = BranchLinkBuilder.Build([step]);

        result.VirtualStops.Values.Single().IsSilent.Should().BeTrue();
    }

    [Fact]
    public void When_code_creates_separate_link_per_entry()
    {
        var step = new BaseStep
        {
            Id = "x", StepKind = "k",
            OnFail = new Branch
            {
                Decision = Decision.NextStep,
                WhenCode = ImmutableDictionary<int, Branch>.Empty
                    .Add(5600, new Branch { Decision = Decision.BreakWorkflow, ErrorCode = 5600 })
                    .Add(5700, new Branch { Decision = Decision.SilentBreakWorkflow })
            }
        };
        var next = EditorTestData.Base(id: "next");

        var result = BranchLinkBuilder.Build([step, next]);

        var whenCodeLinks = result.Links.Values
            .Where(l => l.Kind == EditorLinkKind.WhenCode).ToList();
        whenCodeLinks.Should().HaveCount(2);
        whenCodeLinks.Select(l => l.WhenCode).Should().BeEquivalentTo(new int?[] { 5600, 5700 });

        // OnFail.NextStep тоже превращается в линк к следующему шагу.
        var onFail = result.Links.Values.Single(l => l.Kind == EditorLinkKind.OnFail);
        onFail.TargetStepId.Should().Be("next");
    }

    [Fact]
    public void Rebuild_for_step_replaces_only_outgoing_links_of_that_step()
    {
        var a = new BaseStep { Id = "a", StepKind = "x" };
        var b = new BaseStep { Id = "b", StepKind = "y" };
        var initial = BranchLinkBuilder.Build([a, b]);

        var aWithBranch = a with
        {
            OnSuccess = new Branch { Decision = Decision.SilentBreakWorkflow }
        };
        var (newLinks, newStops) = BranchLinkBuilder.RebuildForStep(
            "a", [aWithBranch, b], initial.Links, initial.VirtualStops);

        // У "a" больше нет линейной связи — теперь связь в STOP-узел.
        newLinks.Values.Where(l => l.SourceStepId == "a")
            .Should().ContainSingle()
            .Which.Decision.Should().Be(Decision.SilentBreakWorkflow);
        newStops.Values.Where(s => s.OwnerStepId == "a").Should().HaveCount(1);
    }

    [Fact]
    public void Stop_id_is_deterministic_for_same_kind_and_when_code()
    {
        var id1 = BranchLinkBuilder.StopId("step1", EditorLinkKind.OnSuccess, null);
        var id2 = BranchLinkBuilder.StopId("step1", EditorLinkKind.OnSuccess, null);
        var id3 = BranchLinkBuilder.StopId("step1", EditorLinkKind.WhenCode, 5600);

        id1.Should().Be(id2);
        id1.Should().NotBe(id3);
        BranchLinkBuilder.IsStopId(id1).Should().BeTrue();
        BranchLinkBuilder.IsStopId("regular_id").Should().BeFalse();
    }
}
