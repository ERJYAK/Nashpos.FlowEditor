using System.Collections.Immutable;
using WorkflowEditor.Client.Store.Editor;
using WorkflowEditor.Core.Models;
using static WorkflowEditor.Tests.Client.TestKit.EditorTestData;

namespace WorkflowEditor.Tests.Client.Store.Editor;

public class CloseTabReducerTests
{
    [Fact]
    public void CloseTab_for_unknown_workflow_returns_state_unchanged()
    {
        var doc = Document("wf-1", BaseStep("s-1"));
        var state = StateWith(doc);

        var next = EditorReducers.ReduceCloseTabAction(state, new CloseTabAction("wf-missing"));

        next.Should().BeSameAs(state);
    }

    [Fact]
    public void CloseTab_removes_document_and_clears_dirty()
    {
        var doc = Document("wf-1", BaseStep("s-1"));
        var state = StateWith(doc) with
        {
            DirtyDocuments = ImmutableHashSet<string>.Empty.Add("wf-1")
        };

        var next = EditorReducers.ReduceCloseTabAction(state, new CloseTabAction("wf-1"));

        next.OpenDocuments.Should().NotContainKey("wf-1");
        next.DirtyDocuments.Should().NotContain("wf-1");
        next.ActiveDocumentId.Should().BeNull();
    }

    [Fact]
    public void CloseTab_when_active_switches_active_to_remaining_document()
    {
        var doc1 = Document("wf-1", BaseStep("s-1"));
        var doc2 = Document("wf-2", BaseStep("s-2"));
        var state = new EditorState() with
        {
            OpenDocuments = ImmutableDictionary<string, WorkflowDocument>.Empty
                .SetItem("wf-1", doc1)
                .SetItem("wf-2", doc2),
            ActiveDocumentId = "wf-1"
        };

        var next = EditorReducers.ReduceCloseTabAction(state, new CloseTabAction("wf-1"));

        next.OpenDocuments.Keys.Should().BeEquivalentTo("wf-2");
        next.ActiveDocumentId.Should().Be("wf-2");
    }

    [Fact]
    public void CloseTab_when_inactive_keeps_current_active()
    {
        var doc1 = Document("wf-1", BaseStep("s-1"));
        var doc2 = Document("wf-2", BaseStep("s-2"));
        var state = new EditorState() with
        {
            OpenDocuments = ImmutableDictionary<string, WorkflowDocument>.Empty
                .SetItem("wf-1", doc1)
                .SetItem("wf-2", doc2),
            ActiveDocumentId = "wf-2"
        };

        var next = EditorReducers.ReduceCloseTabAction(state, new CloseTabAction("wf-1"));

        next.ActiveDocumentId.Should().Be("wf-2");
    }
}
