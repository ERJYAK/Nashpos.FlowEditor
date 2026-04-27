using WorkflowEditor.Client.Store.Editor;
using WorkflowEditor.Tests.Client.TestKit;

namespace WorkflowEditor.Tests.Client.Store.Editor;

public class LinkReducerTests
{
    [Fact]
    public void AddLink_records_new_link_and_marks_dirty()
    {
        var state = EditorReducers.ReduceOpenWorkflowAction(new EditorState(),
            new OpenWorkflowAction(EditorTestData.Document("import")));

        var link = new EditorLink { Id = "l1", SourceStepId = "a", TargetStepId = "b" };
        state = EditorReducers.ReduceAddLinkAction(state, new AddLinkAction("import", link));

        state.OpenDocuments["import"].Links["l1"].Should().BeSameAs(link);
        state.IsDirty("import").Should().BeTrue();
    }

    [Fact]
    public void RemoveLinks_drops_specified_links()
    {
        var state = EditorReducers.ReduceOpenWorkflowAction(new EditorState(),
            new OpenWorkflowAction(EditorTestData.Document("import")));

        var l1 = new EditorLink { Id = "l1", SourceStepId = "a", TargetStepId = "b" };
        var l2 = new EditorLink { Id = "l2", SourceStepId = "b", TargetStepId = "c" };
        state = EditorReducers.ReduceAddLinkAction(state, new AddLinkAction("import", l1));
        state = EditorReducers.ReduceAddLinkAction(state, new AddLinkAction("import", l2));

        state = EditorReducers.ReduceRemoveLinksAction(state, new RemoveLinksAction("import", ["l1"]));

        state.OpenDocuments["import"].Links.Keys.Should().BeEquivalentTo(["l2"]);
    }
}
