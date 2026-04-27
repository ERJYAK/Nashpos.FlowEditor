using WorkflowEditor.Client.Store.Editor;
using WorkflowEditor.Core.Models;
using WorkflowEditor.Core.Models.Steps;
using static WorkflowEditor.Tests.Client.TestKit.EditorTestData;

namespace WorkflowEditor.Tests.Client.Store.Editor;

public class StepReducerTests
{
    [Fact]
    public void AddStep_appends_step_to_active_document()
    {
        var doc = Document("wf-1", BaseStep("s-1"));
        var state = StateWith(doc);
        var newStep = BaseStep("s-2", "new");

        var next = EditorReducers.ReduceAddStepAction(state, new AddStepAction("wf-1", newStep));

        next.OpenDocuments["wf-1"].Steps.Should().HaveCount(2)
            .And.Contain(s => s.Id == "s-2");
    }

    [Fact]
    public void AddStep_for_unknown_workflow_returns_state_unchanged()
    {
        var doc = Document("wf-1", BaseStep("s-1"));
        var state = StateWith(doc);

        var next = EditorReducers.ReduceAddStepAction(state, new AddStepAction("wf-missing", BaseStep("s-2")));

        next.Should().BeSameAs(state);
    }

    [Fact]
    public void RemoveStep_drops_step_and_its_links()
    {
        var doc = Document("wf-1", BaseStep("s-1"), BaseStep("s-2"))
            .WithLinks(Link("l-1", "s-1", "s-2"), Link("l-2", "s-2", "s-1"));
        var state = StateWith(doc);

        var next = EditorReducers.ReduceRemoveStepAction(state, new RemoveStepAction("wf-1", "s-1"));

        next.OpenDocuments["wf-1"].Steps.Should().ContainSingle().Which.Id.Should().Be("s-2");
        next.OpenDocuments["wf-1"].Links.Should().BeEmpty();
    }

    [Fact]
    public void MoveSteps_with_empty_moves_returns_state_unchanged()
    {
        var doc = Document("wf-1", BaseStep("s-1"));
        var state = StateWith(doc);

        var next = EditorReducers.ReduceMoveStepsAction(
            state,
            new MoveStepsAction("wf-1", Array.Empty<(string, CanvasPosition)>()));

        next.Should().BeSameAs(state);
    }

    [Fact]
    public void MoveSteps_updates_only_changed_positions()
    {
        var doc = Document("wf-1",
            BaseStep("s-1", x: 10, y: 20),
            BaseStep("s-2", x: 30, y: 40));
        var state = StateWith(doc);

        var next = EditorReducers.ReduceMoveStepsAction(state,
            new MoveStepsAction("wf-1", new (string, CanvasPosition)[]
            {
                ("s-1", new CanvasPosition(100, 200)),
                ("s-2", new CanvasPosition(30, 40))
            }));

        next.OpenDocuments["wf-1"].Steps.First(s => s.Id == "s-1").Position
            .Should().Be(new CanvasPosition(100, 200));
        next.OpenDocuments["wf-1"].Steps.First(s => s.Id == "s-2").Position
            .Should().Be(new CanvasPosition(30, 40));
    }

    [Fact]
    public void MoveSteps_when_nothing_actually_moved_returns_state_unchanged()
    {
        var doc = Document("wf-1", BaseStep("s-1", x: 10, y: 20));
        var state = StateWith(doc);

        var next = EditorReducers.ReduceMoveStepsAction(state,
            new MoveStepsAction("wf-1", new (string, CanvasPosition)[]
            {
                ("s-1", new CanvasPosition(10, 20))
            }));

        next.Should().BeSameAs(state);
    }

    [Fact]
    public void RenameStep_preserves_BaseStep_type()
    {
        var doc = Document("wf-1", BaseStep("s-1", "old"));
        var state = StateWith(doc);

        var next = EditorReducers.ReduceRenameStepAction(state,
            new RenameStepAction("wf-1", "s-1", "new"));

        var step = next.OpenDocuments["wf-1"].Steps.Single();
        step.Should().BeOfType<BaseStep>();
        step.Name.Should().Be("new");
    }

    [Fact]
    public void RenameStep_preserves_SubflowStep_type_and_subflowId()
    {
        var doc = Document("wf-1", SubflowStep("s-1", "old", subflowId: "sub-42"));
        var state = StateWith(doc);

        var next = EditorReducers.ReduceRenameStepAction(state,
            new RenameStepAction("wf-1", "s-1", "new"));

        var step = next.OpenDocuments["wf-1"].Steps.Single().Should().BeOfType<SubflowStep>().Subject;
        step.Name.Should().Be("new");
        step.SubflowId.Should().Be("sub-42");
    }

    [Fact]
    public void MoveSteps_preserves_SubflowStep_type_and_subflowId()
    {
        var doc = Document("wf-1", SubflowStep("s-1", subflowId: "sub-9", x: 0, y: 0));
        var state = StateWith(doc);

        var next = EditorReducers.ReduceMoveStepsAction(state,
            new MoveStepsAction("wf-1", new (string, CanvasPosition)[]
            {
                ("s-1", new CanvasPosition(50, 60))
            }));

        var step = next.OpenDocuments["wf-1"].Steps.Single().Should().BeOfType<SubflowStep>().Subject;
        step.Position.Should().Be(new CanvasPosition(50, 60));
        step.SubflowId.Should().Be("sub-9");
    }

    [Fact]
    public void StartEditingStep_sets_editing_id()
    {
        var state = new EditorState();

        var next = EditorReducers.ReduceStartEditingStepAction(state, new StartEditingStepAction("s-1"));

        next.EditingStepId.Should().Be("s-1");
    }

    [Fact]
    public void StopEditingStep_clears_editing_id()
    {
        var state = new EditorState() with { EditingStepId = "s-1" };

        var next = EditorReducers.ReduceStopEditingStepAction(state, new StopEditingStepAction());

        next.EditingStepId.Should().BeNull();
    }
}
