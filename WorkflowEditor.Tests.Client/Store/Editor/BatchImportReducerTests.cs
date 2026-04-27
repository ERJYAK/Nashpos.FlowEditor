using WorkflowEditor.Client.Store.Editor;
using WorkflowEditor.Tests.Client.TestKit;

namespace WorkflowEditor.Tests.Client.Store.Editor;

public class BatchImportReducerTests
{
    [Fact]
    public void Started_increments_PendingImports_by_count()
    {
        var state = EditorReducers.ReduceBatchImportStartedAction(new EditorState(), new BatchImportStartedAction(3));

        state.PendingImports.Should().Be(3);
    }

    [Fact]
    public void Three_OpenWorkflow_after_Started_3_brings_PendingImports_to_zero()
    {
        var state = EditorReducers.ReduceBatchImportStartedAction(new EditorState(), new BatchImportStartedAction(3));

        for (var i = 1; i <= 3; i++)
        {
            var doc = EditorTestData.Document($"f{i}", steps: EditorTestData.Base("k"));
            state = EditorReducers.ReduceOpenWorkflowAction(state, new OpenWorkflowAction(doc));
        }

        state.PendingImports.Should().Be(0);
        state.OpenDocuments.Should().HaveCount(3);
    }

    [Fact]
    public void ImportFileFailed_decrements_PendingImports()
    {
        var state = EditorReducers.ReduceBatchImportStartedAction(new EditorState(), new BatchImportStartedAction(2));

        state = EditorReducers.ReduceImportFileFailedAction(state, new ImportFileFailedAction("a.json", "bad"));
        state = EditorReducers.ReduceImportFileFailedAction(state, new ImportFileFailedAction("b.json", "bad"));

        state.PendingImports.Should().Be(0);
    }

    [Fact]
    public void OpenWorkflow_when_no_batch_pending_does_not_underflow()
    {
        var state = new EditorState();
        var doc = EditorTestData.Document("standalone", steps: EditorTestData.Base("k"));

        state = EditorReducers.ReduceOpenWorkflowAction(state, new OpenWorkflowAction(doc));

        state.PendingImports.Should().Be(0);
    }
}
