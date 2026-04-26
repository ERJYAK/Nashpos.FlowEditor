using WorkflowEditor.Client.Store.Editor;
using WorkflowEditor.Core.Models;
using WorkflowEditor.Tests.Client.TestKit;
using static WorkflowEditor.Tests.Client.TestKit.EditorTestData;

namespace WorkflowEditor.Tests.Client.Store.Editor;

public class TabLifecycleReducerTests
{
    [Fact]
    public void OpenWorkflow_adds_document_and_sets_active()
    {
        var state = new EditorState();
        var doc = Document("wf-1", BaseStep("s-1"));

        var next = EditorReducers.ReduceOpenWorkflowAction(state, new OpenWorkflowAction(doc));

        next.OpenDocuments.Should().ContainKey("wf-1").WhoseValue.Should().BeSameAs(doc);
        next.ActiveDocumentId.Should().Be("wf-1");
    }

    [Fact]
    public void OpenWorkflow_replaces_existing_document_with_same_id()
    {
        var original = Document("wf-1", BaseStep("s-1", "old"));
        var state = StateWith(original);
        var updated = Document("wf-1", BaseStep("s-1", "new"));

        var next = EditorReducers.ReduceOpenWorkflowAction(state, new OpenWorkflowAction(updated));

        next.OpenDocuments["wf-1"].Should().BeSameAs(updated);
    }

    [Fact]
    public void SwitchTab_changes_active_document()
    {
        var doc = Document("wf-1");
        var state = StateWith(doc, activeId: "other");

        var next = EditorReducers.ReduceSwitchTabAction(state, new SwitchTabAction("wf-1"));

        next.ActiveDocumentId.Should().Be("wf-1");
    }

    [Fact]
    public void LoadWorkflow_sets_loading_flag()
    {
        var state = new EditorState();

        var next = EditorReducers.ReduceLoadWorkflowAction(state, new LoadWorkflowAction("wf-1"));

        next.IsLoading.Should().BeTrue();
    }

    [Fact]
    public void LoadWorkflowSuccess_clears_loading_and_activates_document()
    {
        var state = new EditorState() with { IsLoading = true };
        var doc = Document("wf-1");

        var next = EditorReducers.ReduceLoadWorkflowSuccessAction(state, new LoadWorkflowSuccessAction(doc));

        next.IsLoading.Should().BeFalse();
        next.OpenDocuments.Should().ContainKey("wf-1");
        next.ActiveDocumentId.Should().Be("wf-1");
    }

    [Fact]
    public void LoadWorkflowFailed_clears_loading()
    {
        var state = new EditorState() with { IsLoading = true };

        var next = EditorReducers.ReduceLoadWorkflowFailedAction(state, new LoadWorkflowFailedAction("oops"));

        next.IsLoading.Should().BeFalse();
    }
}
