using Blazor.Diagrams.Core.Models.Base;
using WorkflowEditor.Client.Diagram.Nodes;
using WorkflowEditor.Client.Store.Editor;
using WorkflowEditor.Core.Models;
using WorkflowEditor.Core.Models.Steps;
using static WorkflowEditor.Tests.Client.TestKit.EditorTestData;

namespace WorkflowEditor.Tests.Client.Store.Editor;

public class ClipboardReducerTests
{
    private static IReadOnlyList<SelectableModel> Selection(params WorkflowStep[] steps) =>
        steps.Select(WorkflowNodeFor).Cast<SelectableModel>().ToList();

    private static WorkflowNodeModel WorkflowNodeFor(WorkflowStep step) => step switch
    {
        SubflowStep s => new SubflowNodeModel(s),
        BaseStep b => new BaseNodeModel(b),
        _ => throw new InvalidOperationException($"unsupported step type {step.GetType().Name}")
    };

    [Fact]
    public void CopySelection_stores_selected_steps_with_min_origin()
    {
        var s1 = BaseStep("s-1", x: 30, y: 40);
        var s2 = BaseStep("s-2", x: 10, y: 80);
        var doc = Document("wf-1", s1, s2);
        var state = StateWith(doc);

        var next = EditorReducers.ReduceCopySelectionAction(state, new CopySelectionAction(Selection(s1, s2)));

        next.Clipboard.Should().NotBeNull();
        next.Clipboard!.Steps.Should().HaveCount(2);
        next.Clipboard.Origin.Should().Be(new CanvasPosition(10, 40));
    }

    [Fact]
    public void CopySelection_includes_links_between_selected_nodes()
    {
        var s1 = BaseStep("s-1");
        var s2 = BaseStep("s-2");
        var s3 = BaseStep("s-3");
        var doc = Document("wf-1", s1, s2, s3)
            .WithLinks(
                Link("l-12", "s-1", "s-2"),
                Link("l-13", "s-1", "s-3"));
        var state = StateWith(doc);

        var next = EditorReducers.ReduceCopySelectionAction(state, new CopySelectionAction(Selection(s1, s2)));

        next.Clipboard!.Links.Should().ContainSingle().Which.Id.Should().Be("l-12");
    }

    [Fact]
    public void CopySelection_with_empty_selection_returns_state_unchanged()
    {
        var doc = Document("wf-1", BaseStep("s-1"));
        var state = StateWith(doc);

        var next = EditorReducers.ReduceCopySelectionAction(state,
            new CopySelectionAction(Array.Empty<SelectableModel>()));

        next.Should().BeSameAs(state);
    }

    [Fact]
    public void PasteClipboard_clones_steps_with_new_ids_and_offset_positions()
    {
        var original = BaseStep("orig", "Task", x: 10, y: 20);
        var doc = Document("wf-1");
        var state = StateWith(doc) with
        {
            Clipboard = new ClipboardPayload(
                Steps: new[] { original },
                Links: Array.Empty<WorkflowLink>(),
                Origin: new CanvasPosition(10, 20))
        };

        var next = EditorReducers.ReducePasteClipboardAction(state, new PasteClipboardAction(100, 200));

        var pasted = next.OpenDocuments["wf-1"].Steps.Should().ContainSingle().Subject;
        pasted.Id.Should().NotBe("orig");
        pasted.Position.Should().Be(new CanvasPosition(100, 200));
        pasted.Name.Should().Be("Task (Copy)");
    }

    [Fact]
    public void PasteClipboard_remaps_link_endpoints_to_new_step_ids()
    {
        var s1 = BaseStep("s-1", x: 0, y: 0);
        var s2 = BaseStep("s-2", x: 50, y: 0);
        var doc = Document("wf-1");
        var state = StateWith(doc) with
        {
            Clipboard = new ClipboardPayload(
                Steps: new WorkflowStep[] { s1, s2 },
                Links: new[] { Link("l-1", "s-1", "s-2") },
                Origin: new CanvasPosition(0, 0))
        };

        var next = EditorReducers.ReducePasteClipboardAction(state, new PasteClipboardAction(100, 100));

        var pastedDoc = next.OpenDocuments["wf-1"];
        pastedDoc.Steps.Should().HaveCount(2);
        var link = pastedDoc.Links.Should().ContainSingle().Subject;
        link.SourceNodeId.Should().NotBe("s-1");
        link.TargetNodeId.Should().NotBe("s-2");
        pastedDoc.Steps.Should().Contain(s => s.Id == link.SourceNodeId);
        pastedDoc.Steps.Should().Contain(s => s.Id == link.TargetNodeId);
    }

    [Fact]
    public void PasteClipboard_without_clipboard_returns_state_unchanged()
    {
        var state = StateWith(Document("wf-1"));

        var next = EditorReducers.ReducePasteClipboardAction(state, new PasteClipboardAction(0, 0));

        next.Should().BeSameAs(state);
    }

    [Fact]
    public void DeleteSelection_removes_selected_nodes_and_their_links()
    {
        var s1 = BaseStep("s-1");
        var s2 = BaseStep("s-2");
        var doc = Document("wf-1", s1, s2)
            .WithLinks(Link("l-12", "s-1", "s-2"));
        var state = StateWith(doc);

        var next = EditorReducers.ReduceDeleteSelectionAction(state,
            new DeleteSelectionAction(Selection(s1)));

        next.OpenDocuments["wf-1"].Steps.Should().ContainSingle().Which.Id.Should().Be("s-2");
        next.OpenDocuments["wf-1"].Links.Should().BeEmpty();
    }

    [Fact]
    public void DeleteSelection_with_empty_selection_returns_state_unchanged()
    {
        var doc = Document("wf-1", BaseStep("s-1"));
        var state = StateWith(doc);

        var next = EditorReducers.ReduceDeleteSelectionAction(state,
            new DeleteSelectionAction(Array.Empty<SelectableModel>()));

        next.Should().BeSameAs(state);
    }
}
