using WorkflowEditor.Client.Store.Editor;
using WorkflowEditor.Tests.Client.TestKit;

namespace WorkflowEditor.Tests.Client.Store.Editor;

public class SubflowCacheReducerTests
{
    // Subflow-узлы показывают вложенные шаги из SubflowCache. Кэш наполняется при любом
    // открытии/импорте документа — это и есть «сессионный» источник данных для subflow.
    [Fact]
    public void OpenWorkflow_caches_document_for_subflow_preview()
    {
        var doc = EditorTestData.Document("prepare-import", steps: EditorTestData.Base("k"));

        var state = EditorReducers.ReduceOpenWorkflowAction(new EditorState(), new OpenWorkflowAction(doc));

        state.SubflowCache.Should().ContainKey("prepare-import");
        state.SubflowCache["prepare-import"].Should().BeSameAs(doc);
    }
}
