using WorkflowEditor.Client.Store.Editor;
using WorkflowEditor.Core.Models;
using static WorkflowEditor.Tests.Client.TestKit.EditorTestData;

namespace WorkflowEditor.Tests.Client.Store.Editor;

public class DirtyFlagReducerTests
{
    [Fact]
    public void AddStep_marks_document_dirty()
    {
        var doc = Document("wf-1", BaseStep("s-1"));
        var state = StateWith(doc);

        var next = EditorReducers.ReduceAddStepAction(state, new AddStepAction("wf-1", BaseStep("s-2")));

        next.IsDirty("wf-1").Should().BeTrue();
    }

    [Fact]
    public void MoveSteps_marks_document_dirty()
    {
        var doc = Document("wf-1", BaseStep("s-1", x: 0, y: 0));
        var state = StateWith(doc);

        var next = EditorReducers.ReduceMoveStepsAction(state,
            new MoveStepsAction("wf-1", new (string, CanvasPosition)[]
            {
                ("s-1", new CanvasPosition(10, 20))
            }));

        next.IsDirty("wf-1").Should().BeTrue();
    }

    [Fact]
    public void RenameStep_marks_document_dirty()
    {
        var doc = Document("wf-1", BaseStep("s-1", "old"));
        var state = StateWith(doc);

        var next = EditorReducers.ReduceRenameStepAction(state, new RenameStepAction("wf-1", "s-1", "new"));

        next.IsDirty("wf-1").Should().BeTrue();
    }

    [Fact]
    public void RenameStep_with_same_name_keeps_state_clean()
    {
        var doc = Document("wf-1", BaseStep("s-1", "same"));
        var state = StateWith(doc);

        var next = EditorReducers.ReduceRenameStepAction(state, new RenameStepAction("wf-1", "s-1", "same"));

        next.IsDirty("wf-1").Should().BeFalse();
        next.Should().BeSameAs(state);
    }

    [Fact]
    public void AddLink_marks_document_dirty()
    {
        var doc = Document("wf-1", BaseStep("s-1"), BaseStep("s-2"));
        var state = StateWith(doc);

        var next = EditorReducers.ReduceAddLinkAction(state, new AddLinkAction("wf-1", Link("l-1", "s-1", "s-2")));

        next.IsDirty("wf-1").Should().BeTrue();
    }

    [Fact]
    public void RemoveLink_for_unknown_link_keeps_state_clean()
    {
        var doc = Document("wf-1", BaseStep("s-1"));
        var state = StateWith(doc);

        var next = EditorReducers.ReduceRemoveLinkAction(state, new RemoveLinkAction("wf-1", "ghost"));

        next.IsDirty("wf-1").Should().BeFalse();
        next.Should().BeSameAs(state);
    }

    [Fact]
    public void SaveWorkflowSuccess_clears_dirty_for_that_document()
    {
        var doc = Document("wf-1", BaseStep("s-1"));
        var state = StateWith(doc);
        var dirty = EditorReducers.ReduceAddStepAction(state, new AddStepAction("wf-1", BaseStep("s-2")));

        var next = EditorReducers.ReduceSaveWorkflowSuccessAction(dirty, new SaveWorkflowSuccessAction("wf-1"));

        next.IsDirty("wf-1").Should().BeFalse();
    }

    [Fact]
    public void LoadWorkflowSuccess_clears_dirty_for_reloaded_document()
    {
        var doc = Document("wf-1", BaseStep("s-1"));
        var dirty = StateWith(doc) with
        {
            DirtyDocuments = System.Collections.Immutable.ImmutableHashSet<string>.Empty.Add("wf-1")
        };

        var next = EditorReducers.ReduceLoadWorkflowSuccessAction(dirty, new LoadWorkflowSuccessAction(doc));

        next.IsDirty("wf-1").Should().BeFalse();
    }

    [Fact]
    public void OpenWorkflow_clears_dirty_for_replaced_document()
    {
        var doc = Document("wf-1", BaseStep("s-1"));
        var dirty = StateWith(doc) with
        {
            DirtyDocuments = System.Collections.Immutable.ImmutableHashSet<string>.Empty.Add("wf-1")
        };

        var next = EditorReducers.ReduceOpenWorkflowAction(dirty, new OpenWorkflowAction(doc));

        next.IsDirty("wf-1").Should().BeFalse();
    }
}
