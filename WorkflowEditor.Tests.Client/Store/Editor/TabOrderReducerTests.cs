using WorkflowEditor.Client.Store.Editor;
using WorkflowEditor.Tests.Client.TestKit;

namespace WorkflowEditor.Tests.Client.Store.Editor;

public class TabOrderReducerTests
{
    [Fact]
    public void Open_appends_to_TabOrder()
    {
        var state = new EditorState();
        state = EditorReducers.ReduceOpenWorkflowAction(state,
            new OpenWorkflowAction(EditorTestData.Document("a", steps: EditorTestData.Base("k"))));
        state = EditorReducers.ReduceOpenWorkflowAction(state,
            new OpenWorkflowAction(EditorTestData.Document("b", steps: EditorTestData.Base("k"))));

        state.TabOrder.Should().ContainInOrder("a", "b");
    }

    [Fact]
    public void Reopening_existing_does_not_duplicate_TabOrder()
    {
        var state = EditorReducers.ReduceOpenWorkflowAction(new EditorState(),
            new OpenWorkflowAction(EditorTestData.Document("a", steps: EditorTestData.Base("k"))));
        state = EditorReducers.ReduceOpenWorkflowAction(state,
            new OpenWorkflowAction(EditorTestData.Document("a", steps: EditorTestData.Base("k"))));

        state.TabOrder.Should().BeEquivalentTo(new[] { "a" });
    }

    [Fact]
    public void Close_removes_from_TabOrder()
    {
        var state = EditorReducers.ReduceOpenWorkflowAction(new EditorState(),
            new OpenWorkflowAction(EditorTestData.Document("a", steps: EditorTestData.Base("k"))));
        state = EditorReducers.ReduceOpenWorkflowAction(state,
            new OpenWorkflowAction(EditorTestData.Document("b", steps: EditorTestData.Base("k"))));

        state = EditorReducers.ReduceCloseTabAction(state, new CloseTabAction("a"));

        state.TabOrder.Should().BeEquivalentTo(new[] { "b" });
    }

    [Fact]
    public void ReorderTabs_sets_new_order_filtering_unknown_names()
    {
        var state = EditorReducers.ReduceOpenWorkflowAction(new EditorState(),
            new OpenWorkflowAction(EditorTestData.Document("a", steps: EditorTestData.Base("k"))));
        state = EditorReducers.ReduceOpenWorkflowAction(state,
            new OpenWorkflowAction(EditorTestData.Document("b", steps: EditorTestData.Base("k"))));
        state = EditorReducers.ReduceOpenWorkflowAction(state,
            new OpenWorkflowAction(EditorTestData.Document("c", steps: EditorTestData.Base("k"))));

        state = EditorReducers.ReduceReorderTabsAction(state,
            new ReorderTabsAction(["b", "ghost", "a"])); // ghost не открыт — игнор; c забыли — добавится в конец

        state.TabOrder.Should().ContainInOrder("b", "a", "c");
    }
}
