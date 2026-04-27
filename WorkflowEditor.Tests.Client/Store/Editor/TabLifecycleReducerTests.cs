using WorkflowEditor.Client.Store.Editor;
using WorkflowEditor.Tests.Client.TestKit;

namespace WorkflowEditor.Tests.Client.Store.Editor;

public class TabLifecycleReducerTests
{
    [Fact]
    public void Open_adds_document_and_makes_it_active()
    {
        var doc = EditorTestData.Document("import", "Import flow", EditorTestData.Base("apply-import"));

        var state = EditorReducers.ReduceOpenWorkflowAction(new EditorState(), new OpenWorkflowAction(doc));

        state.OpenDocuments.Keys.Should().Contain("import");
        state.ActiveDocumentName.Should().Be("import");
        state.IsDirty("import").Should().BeFalse();
    }

    [Fact]
    public void Switch_changes_active_document()
    {
        var doc1 = EditorTestData.Document("a", steps: EditorTestData.Base("k"));
        var doc2 = EditorTestData.Document("b", steps: EditorTestData.Base("k"));
        var state = EditorReducers.ReduceOpenWorkflowAction(new EditorState(), new OpenWorkflowAction(doc1));
        state = EditorReducers.ReduceOpenWorkflowAction(state, new OpenWorkflowAction(doc2));

        state = EditorReducers.ReduceSwitchTabAction(state, new SwitchTabAction("a"));

        state.ActiveDocumentName.Should().Be("a");
    }

    [Fact]
    public void Close_removes_document_and_picks_first_remaining_as_active()
    {
        var doc1 = EditorTestData.Document("a", steps: EditorTestData.Base("k"));
        var doc2 = EditorTestData.Document("b", steps: EditorTestData.Base("k"));
        var state = EditorReducers.ReduceOpenWorkflowAction(new EditorState(), new OpenWorkflowAction(doc1));
        state = EditorReducers.ReduceOpenWorkflowAction(state, new OpenWorkflowAction(doc2));

        state = EditorReducers.ReduceCloseTabAction(state, new CloseTabAction("b"));

        state.OpenDocuments.Should().ContainSingle().Which.Key.Should().Be("a");
        state.ActiveDocumentName.Should().Be("a");
    }

    [Fact]
    public void Open_with_existing_steps_creates_linear_links_between_consecutive_steps()
    {
        var doc = EditorTestData.Document("import", "",
            EditorTestData.Base("a", id: "1"),
            EditorTestData.Base("b", id: "2"),
            EditorTestData.Base("c", id: "3"));

        var state = EditorReducers.ReduceOpenWorkflowAction(new EditorState(), new OpenWorkflowAction(doc));

        var editor = state.OpenDocuments["import"];
        editor.Links.Values.Should().HaveCount(2);
        editor.Links.Values.Select(l => (l.SourceStepId, l.TargetStepId))
            .Should().BeEquivalentTo(new[] { ("1", "2"), ("2", "3") });
        editor.NodePositions.Should().HaveCount(3);
    }
}
