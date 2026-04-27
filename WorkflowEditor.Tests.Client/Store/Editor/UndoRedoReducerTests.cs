using WorkflowEditor.Client.Store.Editor;
using WorkflowEditor.Core.Models;
using static WorkflowEditor.Tests.Client.TestKit.EditorTestData;

namespace WorkflowEditor.Tests.Client.Store.Editor;

public class UndoRedoReducerTests
{
    [Fact]
    public void Mutation_pushes_previous_document_onto_undo_stack()
    {
        var doc = Document("wf-1", BaseStep("s-1"));
        var state = StateWith(doc);

        var afterAdd = EditorReducers.ReduceAddStepAction(state, new AddStepAction("wf-1", BaseStep("s-2")));

        afterAdd.CanUndo("wf-1").Should().BeTrue();
        afterAdd.CanRedo("wf-1").Should().BeFalse();
    }

    [Fact]
    public void Undo_restores_previous_document_and_enables_redo()
    {
        var doc = Document("wf-1", BaseStep("s-1"));
        var state = StateWith(doc);
        var afterAdd = EditorReducers.ReduceAddStepAction(state, new AddStepAction("wf-1", BaseStep("s-2")));

        var undone = EditorReducers.ReduceUndoAction(afterAdd, new UndoAction("wf-1"));

        undone.OpenDocuments["wf-1"].Steps.Should().HaveCount(1);
        undone.OpenDocuments["wf-1"].Steps.Should().ContainKey("s-1");
        undone.CanUndo("wf-1").Should().BeFalse();
        undone.CanRedo("wf-1").Should().BeTrue();
    }

    [Fact]
    public void Redo_restores_post_mutation_document()
    {
        var doc = Document("wf-1", BaseStep("s-1"));
        var state = StateWith(doc);
        var afterAdd = EditorReducers.ReduceAddStepAction(state, new AddStepAction("wf-1", BaseStep("s-2")));
        var undone = EditorReducers.ReduceUndoAction(afterAdd, new UndoAction("wf-1"));

        var redone = EditorReducers.ReduceRedoAction(undone, new RedoAction("wf-1"));

        redone.OpenDocuments["wf-1"].Steps.Should().HaveCount(2);
        redone.OpenDocuments["wf-1"].Steps.Should().ContainKey("s-2");
        redone.CanUndo("wf-1").Should().BeTrue();
        redone.CanRedo("wf-1").Should().BeFalse();
    }

    [Fact]
    public void Mutation_after_undo_clears_redo_stack()
    {
        var doc = Document("wf-1", BaseStep("s-1"));
        var state = StateWith(doc);
        var afterAdd = EditorReducers.ReduceAddStepAction(state, new AddStepAction("wf-1", BaseStep("s-2")));
        var undone = EditorReducers.ReduceUndoAction(afterAdd, new UndoAction("wf-1"));

        var afterAnotherMutation = EditorReducers.ReduceAddStepAction(undone,
            new AddStepAction("wf-1", BaseStep("s-3")));

        afterAnotherMutation.CanRedo("wf-1").Should().BeFalse();
    }

    [Fact]
    public void Undo_with_empty_stack_returns_state_unchanged()
    {
        var doc = Document("wf-1", BaseStep("s-1"));
        var state = StateWith(doc);

        var next = EditorReducers.ReduceUndoAction(state, new UndoAction("wf-1"));

        next.Should().BeSameAs(state);
    }

    [Fact]
    public void OpenWorkflow_clears_both_undo_and_redo_stacks()
    {
        var doc = Document("wf-1", BaseStep("s-1"));
        var state = StateWith(doc);
        var dirty = EditorReducers.ReduceAddStepAction(state, new AddStepAction("wf-1", BaseStep("s-2")));

        var reopened = EditorReducers.ReduceOpenWorkflowAction(dirty, new OpenWorkflowAction(doc));

        reopened.CanUndo("wf-1").Should().BeFalse();
        reopened.CanRedo("wf-1").Should().BeFalse();
    }

    [Fact]
    public void CloseTab_drops_undo_redo_for_that_document()
    {
        var doc = Document("wf-1", BaseStep("s-1"));
        var state = StateWith(doc);
        var afterAdd = EditorReducers.ReduceAddStepAction(state, new AddStepAction("wf-1", BaseStep("s-2")));

        var closed = EditorReducers.ReduceCloseTabAction(afterAdd, new CloseTabAction("wf-1"));

        closed.UndoStacks.Should().NotContainKey("wf-1");
        closed.RedoStacks.Should().NotContainKey("wf-1");
    }
}
