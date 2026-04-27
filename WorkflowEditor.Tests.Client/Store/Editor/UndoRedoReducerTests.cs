using WorkflowEditor.Client.Store.Editor;
using WorkflowEditor.Core.Models;
using WorkflowEditor.Core.Models.Steps;
using WorkflowEditor.Tests.Client.TestKit;

namespace WorkflowEditor.Tests.Client.Store.Editor;

public class UndoRedoReducerTests
{
    private static EditorState Open(WorkflowDocument doc) =>
        EditorReducers.ReduceOpenWorkflowAction(new EditorState(), new OpenWorkflowAction(doc));

    [Fact]
    public void Undo_after_AddStep_restores_previous_state_and_pushes_to_redo()
    {
        var state = Open(EditorTestData.Document("import"));
        state = EditorReducers.ReduceAddStepAction(state,
            new AddStepAction("import",
                new BaseStep { Id = "x", StepKind = "k", Description = "d" },
                new CanvasPosition(0, 0)));

        state.CanUndo("import").Should().BeTrue();
        state = EditorReducers.ReduceUndoAction(state, new UndoAction("import"));

        state.OpenDocuments["import"].Document.Steps.Should().BeEmpty();
        state.CanRedo("import").Should().BeTrue();
    }

    [Fact]
    public void Redo_after_undo_re_applies_change()
    {
        var state = Open(EditorTestData.Document("import"));
        state = EditorReducers.ReduceAddStepAction(state,
            new AddStepAction("import",
                new BaseStep { Id = "x", StepKind = "k", Description = "d" },
                new CanvasPosition(0, 0)));
        state = EditorReducers.ReduceUndoAction(state, new UndoAction("import"));
        state = EditorReducers.ReduceRedoAction(state, new RedoAction("import"));

        state.OpenDocuments["import"].Document.Steps.Should().ContainSingle().Which.Id.Should().Be("x");
    }
}
