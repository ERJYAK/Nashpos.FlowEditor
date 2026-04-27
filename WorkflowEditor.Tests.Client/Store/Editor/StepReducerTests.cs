using WorkflowEditor.Client.Store.Editor;
using WorkflowEditor.Core.Models;
using WorkflowEditor.Core.Models.Steps;
using WorkflowEditor.Tests.Client.TestKit;

namespace WorkflowEditor.Tests.Client.Store.Editor;

public class StepReducerTests
{
    private static EditorState Open(WorkflowDocument doc) =>
        EditorReducers.ReduceOpenWorkflowAction(new EditorState(), new OpenWorkflowAction(doc));

    [Fact]
    public void AddStep_appends_step_and_records_position()
    {
        var state = Open(EditorTestData.Document("import"));
        var step = new BaseStep { Id = "x", StepKind = "k", Description = "d" };

        state = EditorReducers.ReduceAddStepAction(state,
            new AddStepAction("import", step, new CanvasPosition(50, 60)));

        var editor = state.OpenDocuments["import"];
        editor.Document.Steps.Should().ContainSingle().Which.Id.Should().Be("x");
        editor.NodePositions["x"].Should().Be(new CanvasPosition(50, 60));
        state.IsDirty("import").Should().BeTrue();
    }

    [Fact]
    public void RemoveSteps_drops_step_with_its_links_and_positions()
    {
        var state = Open(EditorTestData.Document("import", "",
            EditorTestData.Base("a", id: "1"),
            EditorTestData.Base("b", id: "2")));

        state = EditorReducers.ReduceRemoveStepsAction(state, new RemoveStepsAction("import", ["1"]));

        var editor = state.OpenDocuments["import"];
        editor.Document.Steps.Should().ContainSingle().Which.Id.Should().Be("2");
        editor.Links.Should().BeEmpty();
        editor.NodePositions.Keys.Should().NotContain("1");
    }

    [Fact]
    public void UpdateStepDescription_changes_only_description_and_marks_dirty()
    {
        var state = Open(EditorTestData.Document("import", "", EditorTestData.Base("k", "old", id: "x")));

        state = EditorReducers.ReduceUpdateStepDescriptionAction(state,
            new UpdateStepDescriptionAction("import", "x", "new"));

        state.OpenDocuments["import"].Document.Steps[0].Description.Should().Be("new");
        state.IsDirty("import").Should().BeTrue();
    }

    [Fact]
    public void MoveStep_updates_position_without_marking_dirty()
    {
        var state = Open(EditorTestData.Document("import", "", EditorTestData.Base("k", id: "x")));

        state = EditorReducers.ReduceMoveStepAction(state,
            new MoveStepAction("import", "x", new CanvasPosition(100, 200)));

        state.OpenDocuments["import"].NodePositions["x"].Should().Be(new CanvasPosition(100, 200));
        state.IsDirty("import").Should().BeFalse("позиции — UI-only, не уезжают на сервер");
    }
}
