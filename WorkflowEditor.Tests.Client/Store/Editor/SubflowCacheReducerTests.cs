using WorkflowEditor.Client.Store.Editor;
using WorkflowEditor.Tests.Client.TestKit;

namespace WorkflowEditor.Tests.Client.Store.Editor;

public class SubflowCacheReducerTests
{
    [Fact]
    public void LoadSubflow_marks_loading_and_then_caches_on_success()
    {
        var state = EditorReducers.ReduceLoadSubflowAction(new EditorState(), new LoadSubflowAction("prepare-import"));
        state.LoadingSubflows.Should().Contain("prepare-import");

        var loaded = EditorTestData.Document("prepare-import", steps: EditorTestData.Base("k"));
        state = EditorReducers.ReduceLoadSubflowSuccessAction(state, new LoadSubflowSuccessAction("prepare-import", loaded));

        state.LoadingSubflows.Should().NotContain("prepare-import");
        state.SubflowCache.Should().ContainKey("prepare-import");
    }

    [Fact]
    public void LoadSubflowFailed_clears_loading_and_does_not_cache()
    {
        var state = EditorReducers.ReduceLoadSubflowAction(new EditorState(), new LoadSubflowAction("missing"));
        state = EditorReducers.ReduceLoadSubflowFailedAction(state, new LoadSubflowFailedAction("missing", "404"));

        state.LoadingSubflows.Should().NotContain("missing");
        state.SubflowCache.Should().NotContainKey("missing");
    }

    [Fact]
    public void SaveSuccess_invalidates_cache_for_saved_workflow()
    {
        var state = new EditorState() with
        {
            SubflowCache = new EditorState().SubflowCache.SetItem("import", EditorTestData.Document("import"))
        };

        state = EditorReducers.ReduceSaveWorkflowSuccessAction(state, new SaveWorkflowSuccessAction("import"));

        state.SubflowCache.Should().NotContainKey("import");
    }
}
