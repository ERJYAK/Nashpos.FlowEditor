using System.Collections.Immutable;
using WorkflowEditor.Client.Store.Editor;
using WorkflowEditor.Core.Models;
using WorkflowEditor.Core.Models.Steps;
using WorkflowEditor.Tests.Client.TestKit;

namespace WorkflowEditor.Tests.Client.Store.Editor;

public class ClipboardReducerTests
{
    private static EditorState OpenWith(WorkflowDocument doc) =>
        EditorReducers.ReduceOpenWorkflowAction(new EditorState(), new OpenWorkflowAction(doc));

    private static EditorState SetEditor(EditorState state, string name, EditorDocument editor) =>
        state with { OpenDocuments = state.OpenDocuments.SetItem(name, editor) };

    [Fact]
    public void Copy_keeps_only_internal_links_between_selected_steps()
    {
        var a = new BaseStep { Id = "a", StepKind = "k", Description = "A" };
        var b = new BaseStep { Id = "b", StepKind = "k", Description = "B" };
        var c = new BaseStep { Id = "c", StepKind = "k", Description = "C" };

        var state = OpenWith(EditorTestData.Document("doc", "", a, b, c));
        var editor = state.OpenDocuments["doc"];

        // Заменим автогенерированные links на конкретные: a→b и b→c.
        var manual = editor with
        {
            Links = ImmutableDictionary<string, EditorLink>.Empty
                .Add("ab", new EditorLink { Id = "ab", SourceStepId = "a", TargetStepId = "b" })
                .Add("bc", new EditorLink { Id = "bc", SourceStepId = "b", TargetStepId = "c" })
        };
        state = SetEditor(state, "doc", manual);

        // Копируем только a и b — link a→b внутренний, b→c внешний (c не выделен).
        state = EditorReducers.ReduceCopySelectionAction(state, new CopySelectionAction("doc", ["a", "b"]));

        state.Clipboard.Should().NotBeNull();
        state.Clipboard!.Steps.Select(s => s.Id).Should().BeEquivalentTo(["a", "b"]);
        state.Clipboard.Links.Should().ContainSingle().Which.Id.Should().Be("ab");
    }

    [Fact]
    public void Copy_records_origin_at_min_x_min_y_of_selected_steps()
    {
        var a = new BaseStep { Id = "a", StepKind = "k", Description = "" };
        var b = new BaseStep { Id = "b", StepKind = "k", Description = "" };

        var state = OpenWith(EditorTestData.Document("doc", "", a, b));
        var editor = state.OpenDocuments["doc"];
        var withPositions = editor with
        {
            NodePositions = ImmutableDictionary<string, CanvasPosition>.Empty
                .Add("a", new CanvasPosition(50, 200))
                .Add("b", new CanvasPosition(80, 100))
        };
        state = SetEditor(state, "doc", withPositions);

        state = EditorReducers.ReduceCopySelectionAction(state, new CopySelectionAction("doc", ["a", "b"]));

        state.Clipboard!.Origin.Should().Be(new CanvasPosition(50, 100));
    }

    [Fact]
    public void Paste_clones_steps_with_new_ids_and_keeps_originals()
    {
        var a = new BaseStep { Id = "a", StepKind = "download", Description = "Download" };
        var b = new BaseStep { Id = "b", StepKind = "process",  Description = "Process"  };

        var state = OpenWith(EditorTestData.Document("doc", "", a, b));
        state = EditorReducers.ReduceCopySelectionAction(state, new CopySelectionAction("doc", ["a", "b"]));

        state = EditorReducers.ReducePasteClipboardAction(state, new PasteClipboardAction("doc", 100, 100));

        var doc = state.OpenDocuments["doc"].Document;
        doc.Steps.Should().HaveCount(4);
        doc.Steps.Take(2).Select(s => s.Id).Should().BeEquivalentTo(["a", "b"]);
        // Клоны имеют другие Id, но тот же StepKind/Description.
        var clones = doc.Steps.Skip(2).Cast<BaseStep>().ToList();
        clones.Select(s => s.StepKind).Should().BeEquivalentTo(["download", "process"]);
        clones.All(s => s.Id != "a" && s.Id != "b").Should().BeTrue();
    }

    [Fact]
    public void Paste_preserves_relative_positions_anchored_at_cursor()
    {
        var a = new BaseStep { Id = "a", StepKind = "k", Description = "" };
        var b = new BaseStep { Id = "b", StepKind = "k", Description = "" };

        var state = OpenWith(EditorTestData.Document("doc", "", a, b));
        var editor = state.OpenDocuments["doc"];
        var withPositions = editor with
        {
            NodePositions = ImmutableDictionary<string, CanvasPosition>.Empty
                .Add("a", new CanvasPosition(10, 10))
                .Add("b", new CanvasPosition(30, 50))   // delta from origin: (20, 40)
        };
        state = SetEditor(state, "doc", withPositions);
        state = EditorReducers.ReduceCopySelectionAction(state, new CopySelectionAction("doc", ["a", "b"]));

        state = EditorReducers.ReducePasteClipboardAction(state, new PasteClipboardAction("doc", 100, 100));

        var positions = state.OpenDocuments["doc"].NodePositions;
        var clones = state.OpenDocuments["doc"].Document.Steps.Skip(2).ToList();
        positions[clones[0].Id].Should().Be(new CanvasPosition(100, 100));
        positions[clones[1].Id].Should().Be(new CanvasPosition(120, 140));
    }

    [Fact]
    public void Paste_recreates_internal_links_with_new_step_ids()
    {
        var a = new BaseStep { Id = "a", StepKind = "k", Description = "" };
        var b = new BaseStep { Id = "b", StepKind = "k", Description = "" };

        var state = OpenWith(EditorTestData.Document("doc", "", a, b));
        var editor = state.OpenDocuments["doc"];
        var withLink = editor with
        {
            Links = ImmutableDictionary<string, EditorLink>.Empty
                .Add("ab", new EditorLink { Id = "ab", SourceStepId = "a", TargetStepId = "b" })
        };
        state = SetEditor(state, "doc", withLink);
        state = EditorReducers.ReduceCopySelectionAction(state, new CopySelectionAction("doc", ["a", "b"]));

        state = EditorReducers.ReducePasteClipboardAction(state, new PasteClipboardAction("doc", 0, 0));

        var links = state.OpenDocuments["doc"].Links.Values.ToList();
        links.Should().HaveCount(2); // оригинал + клон
        var clones = state.OpenDocuments["doc"].Document.Steps.Skip(2).ToList();
        links.Should().ContainSingle(l => l.SourceStepId == clones[0].Id && l.TargetStepId == clones[1].Id);
    }
}
