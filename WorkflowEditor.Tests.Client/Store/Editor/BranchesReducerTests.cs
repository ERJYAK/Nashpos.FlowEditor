using System.Collections.Immutable;
using WorkflowEditor.Client.Store.Editor;
using WorkflowEditor.Core.Models;
using WorkflowEditor.Core.Models.Steps;
using WorkflowEditor.Tests.Client.TestKit;

namespace WorkflowEditor.Tests.Client.Store.Editor;

public class BranchesReducerTests
{
    private static EditorState Open(WorkflowDocument doc) =>
        EditorReducers.ReduceOpenWorkflowAction(new EditorState(), new OpenWorkflowAction(doc));

    [Fact]
    public void Sets_persistent_id_and_branches_on_a_step_and_marks_dirty()
    {
        var state = Open(EditorTestData.Document("flow", "", EditorTestData.Base("k", id: "u1")));

        state = EditorReducers.ReduceUpdateStepBranchesAction(state, new UpdateStepBranchesAction(
            "flow", "u1", "guard1",
            OnSuccess: new Branch { Decision = Decision.GotoStep, StepId = "next1" },
            OnFail:    new Branch { Decision = Decision.NextStep },
            Breakpoint: null));

        var step = state.OpenDocuments["flow"].Document.Steps[0];
        step.StepId.Should().Be("guard1");
        step.OnSuccess!.Decision.Should().Be(Decision.GotoStep);
        step.OnSuccess.StepId.Should().Be("next1");
        step.OnFail!.Decision.Should().Be(Decision.NextStep);
        state.IsDirty("flow").Should().BeTrue();
    }

    [Fact]
    public void Clears_branches_when_passed_null()
    {
        var initial = new BaseStep
        {
            Id = "u1", StepKind = "k",
            OnSuccess = new Branch { Decision = Decision.SilentBreakWorkflow },
            OnFail = new Branch { Decision = Decision.NextStep },
            Breakpoint = new BreakpointConfig { Set = true, TimeoutMs = 5000 }
        };
        var state = Open(EditorTestData.Document("flow", "", initial));

        state = EditorReducers.ReduceUpdateStepBranchesAction(state, new UpdateStepBranchesAction(
            "flow", "u1", null, OnSuccess: null, OnFail: null, Breakpoint: null));

        var step = state.OpenDocuments["flow"].Document.Steps[0];
        step.StepId.Should().BeNull();
        step.OnSuccess.Should().BeNull();
        step.OnFail.Should().BeNull();
        step.Breakpoint.Should().BeNull();
    }

    [Fact]
    public void When_no_actual_change_is_a_noop_and_does_not_mark_dirty()
    {
        var initial = new BaseStep
        {
            Id = "u1", StepKind = "k",
            OnSuccess = new Branch { Decision = Decision.NextStep }
        };
        var state = Open(EditorTestData.Document("flow", "", initial));
        state.IsDirty("flow").Should().BeFalse();

        var same = new Branch { Decision = Decision.NextStep };
        state = EditorReducers.ReduceUpdateStepBranchesAction(state, new UpdateStepBranchesAction(
            "flow", "u1", null, OnSuccess: same, OnFail: null, Breakpoint: null));

        state.IsDirty("flow").Should().BeFalse();
    }

    [Fact]
    public void Stores_when_code_dictionary_intact()
    {
        var state = Open(EditorTestData.Document("flow", "", EditorTestData.Base("k", id: "u1")));

        var when = ImmutableDictionary<int, Branch>.Empty.Add(5600, new Branch
        {
            Decision = Decision.BreakWorkflow,
            ErrorCode = 5600,
            ErrorMessage = "Not found"
        });

        state = EditorReducers.ReduceUpdateStepBranchesAction(state, new UpdateStepBranchesAction(
            "flow", "u1", null,
            OnSuccess: null,
            OnFail: new Branch { Decision = Decision.NextStep, WhenCode = when },
            Breakpoint: null));

        var step = state.OpenDocuments["flow"].Document.Steps[0];
        step.OnFail!.WhenCode.Should().NotBeNull();
        step.OnFail.WhenCode![5600].Decision.Should().Be(Decision.BreakWorkflow);
        step.OnFail.WhenCode[5600].ErrorCode.Should().Be(5600);
    }
}
