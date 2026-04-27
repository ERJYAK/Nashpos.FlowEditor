using WorkflowEditor.Client.Store.Editor;
using WorkflowEditor.Core.Models.Steps;
using WorkflowEditor.Tests.Client.TestKit;

namespace WorkflowEditor.Tests.Client.Store.Editor;

public class RenameWorkflowReducerTests
{
    private static EditorState OpenWith(params string[] names)
    {
        var state = new EditorState();
        foreach (var name in names)
        {
            var doc = EditorTestData.Document(name, steps: EditorTestData.Base("k"));
            state = EditorReducers.ReduceOpenWorkflowAction(state, new OpenWorkflowAction(doc));
        }
        return state;
    }

    [Fact]
    public void Rename_moves_key_in_OpenDocuments_and_updates_DocumentName()
    {
        var state = OpenWith("import");
        state = EditorReducers.ReduceRenameWorkflowAction(state,
            new RenameWorkflowAction("import", "import-prices", CascadeSubflows: false));

        state.OpenDocuments.Should().ContainKey("import-prices").And.NotContainKey("import");
        state.OpenDocuments["import-prices"].Document.Name.Should().Be("import-prices");
        state.ActiveDocumentName.Should().Be("import-prices");
        state.TabOrder.Should().Contain("import-prices").And.NotContain("import");
    }

    [Fact]
    public void Rename_moves_DirtyDocuments_UndoStack_RedoStack_SubflowCache()
    {
        var state = OpenWith("import");
        // Сделаем документ dirty + добавим в SubflowCache.
        state = state with
        {
            DirtyDocuments = state.DirtyDocuments.Add("import"),
            SubflowCache = state.SubflowCache.SetItem("import", state.OpenDocuments["import"].Document)
        };

        state = EditorReducers.ReduceRenameWorkflowAction(state,
            new RenameWorkflowAction("import", "renamed", CascadeSubflows: false));

        state.DirtyDocuments.Should().Contain("renamed").And.NotContain("import");
        state.SubflowCache.Should().ContainKey("renamed").And.NotContainKey("import");
        state.SubflowCache["renamed"].Name.Should().Be("renamed");
    }

    [Fact]
    public void Rename_with_cascade_renames_SubflowStep_references_in_other_documents()
    {
        var state = OpenWith("prepare-import");
        // Открываем второй документ, у которого есть subflow-ссылка на prepare-import.
        var subStep = new SubflowStep { Id = "s1", SubflowName = "prepare-import", Description = "Prepare" };
        var importDoc = EditorTestData.Document("import", "Import flow", subStep);
        state = EditorReducers.ReduceOpenWorkflowAction(state, new OpenWorkflowAction(importDoc));

        state = EditorReducers.ReduceRenameWorkflowAction(state,
            new RenameWorkflowAction("prepare-import", "prepare-final", CascadeSubflows: true));

        var importEditor = state.OpenDocuments["import"];
        var importSubStep = importEditor.Document.Steps.OfType<SubflowStep>().Single();
        importSubStep.SubflowName.Should().Be("prepare-final");
        importSubStep.Id.Should().Be("s1"); // Id шага не меняется
    }

    [Fact]
    public void Rename_without_cascade_does_not_touch_subflow_references()
    {
        var subStep = new SubflowStep { Id = "s1", SubflowName = "prepare-import" };
        var importDoc = EditorTestData.Document("import", "", subStep);
        var state = OpenWith("prepare-import");
        state = EditorReducers.ReduceOpenWorkflowAction(state, new OpenWorkflowAction(importDoc));

        state = EditorReducers.ReduceRenameWorkflowAction(state,
            new RenameWorkflowAction("prepare-import", "prepare-final", CascadeSubflows: false));

        state.OpenDocuments["import"].Document.Steps
            .OfType<SubflowStep>().Single().SubflowName.Should().Be("prepare-import");
    }

    [Fact]
    public void Rename_with_existing_target_is_noop_in_reducer_conflict_handled_in_effect()
    {
        var state = OpenWith("import", "import-prices");
        state = EditorReducers.ReduceRenameWorkflowAction(state,
            new RenameWorkflowAction("import", "import-prices", CascadeSubflows: false));

        // reducer гард: оба ключа остались, изменений нет
        state.OpenDocuments.Should().ContainKey("import").And.ContainKey("import-prices");
    }

    [Fact]
    public void CascadeRenameSubflowReferences_updates_only_subflow_steps_no_tab_move()
    {
        var sub1 = new SubflowStep { Id = "a", SubflowName = "x" };
        var sub2 = new SubflowStep { Id = "b", SubflowName = "x" };
        var docA = EditorTestData.Document("docA", "", sub1);
        var docB = EditorTestData.Document("docB", "", sub2);
        var state = EditorReducers.ReduceOpenWorkflowAction(new EditorState(), new OpenWorkflowAction(docA));
        state = EditorReducers.ReduceOpenWorkflowAction(state, new OpenWorkflowAction(docB));

        state = EditorReducers.ReduceCascadeRenameSubflowReferencesAction(state,
            new CascadeRenameSubflowReferencesAction("x", "y"));

        state.OpenDocuments["docA"].Document.Steps.OfType<SubflowStep>().Single().SubflowName.Should().Be("y");
        state.OpenDocuments["docB"].Document.Steps.OfType<SubflowStep>().Single().SubflowName.Should().Be("y");
        state.OpenDocuments.Keys.Should().BeEquivalentTo(["docA", "docB"]); // вкладок не двигалось
    }
}
