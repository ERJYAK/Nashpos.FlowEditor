using System.Collections.Immutable;
using System.Text.Json;
using WorkflowEditor.Core.Models;
using WorkflowEditor.Core.Models.Steps;
using WorkflowEditor.Core.Serialization;

namespace WorkflowEditor.Tests.Server.Serialization;

// Round-trip JSON ⇒ модель ⇒ JSON для условных шагов (примеры из ТЗ),
// а также проверка плоских полей брейкпоинта.
public class BranchAndBreakpointSerializationTests
{
    private static readonly JsonSerializerOptions Opts = JsonConfiguration.GetOptions();

    private static T Deserialize<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, Opts)!;

    private static string Serialize<T>(T value) =>
        JsonSerializer.Serialize(value, Opts);

    [Fact]
    public void Reads_subflow_with_silent_break_on_success()
    {
        const string payload = """
            {
              "subflow": "rfid-pre-handling",
              "description": "Special RFID handling",
              "onSuccess": { "decision": "SILENT_BREAK_WORKFLOW" }
            }
            """;

        var step = Deserialize<WorkflowStep>(payload);

        step.Should().BeOfType<SubflowStep>();
        step.OnSuccess.Should().NotBeNull();
        step.OnSuccess!.Decision.Should().Be(Decision.SilentBreakWorkflow);
        step.OnFail.Should().BeNull();
    }

    [Fact]
    public void Reads_goto_step_branch_with_step_id()
    {
        const string payload = """
            {
              "step": "check-transaction-type",
              "description": "Check if SALE",
              "onSuccess": { "decision": "GOTO_STEP", "stepId": "build_sale_barcode" },
              "onFail":    { "decision": "NEXT_STEP" }
            }
            """;

        var step = Deserialize<WorkflowStep>(payload);

        step.OnSuccess!.Decision.Should().Be(Decision.GotoStep);
        step.OnSuccess.StepId.Should().Be("build_sale_barcode");
        step.OnFail!.Decision.Should().Be(Decision.NextStep);
    }

    [Fact]
    public void Reads_step_with_persistent_id()
    {
        const string payload = """
            {
              "step": "execute-js-script",
              "id": "build_sale_barcode",
              "description": "..."
            }
            """;

        var step = Deserialize<WorkflowStep>(payload);

        step.StepId.Should().Be("build_sale_barcode");
    }

    [Fact]
    public void Reads_break_workflow_with_error_code_and_message()
    {
        const string payload = """
            {
              "step": "guard",
              "description": "Block",
              "onSuccess": {
                "decision": "BREAK_WORKFLOW",
                "errorCode": 5000,
                "errorMessage": "Operation not allowed"
              }
            }
            """;

        var step = Deserialize<WorkflowStep>(payload);

        step.OnSuccess!.Decision.Should().Be(Decision.BreakWorkflow);
        step.OnSuccess.ErrorCode.Should().Be(5000);
        step.OnSuccess.ErrorMessage.Should().Be("Operation not allowed");
    }

    [Fact]
    public void Reads_when_code_with_nested_break()
    {
        const string payload = """
            {
              "subflow": "rfid-pre-handling",
              "description": "Special RFID handling",
              "onSuccess": { "decision": "SILENT_BREAK_WORKFLOW" },
              "onFail": {
                "decision": "NEXT_STEP",
                "whenCode": {
                  "5600": {
                    "description": "If productNotFound (5600) we break workflow",
                    "decision": "BREAK_WORKFLOW"
                  }
                }
              }
            }
            """;

        var step = Deserialize<WorkflowStep>(payload);

        step.OnFail!.Decision.Should().Be(Decision.NextStep);
        step.OnFail.WhenCode.Should().NotBeNull();
        step.OnFail.WhenCode![5600].Decision.Should().Be(Decision.BreakWorkflow);
        step.OnFail.WhenCode[5600].Description.Should().Be("If productNotFound (5600) we break workflow");
    }

    [Fact]
    public void Round_trip_preserves_when_code_branch()
    {
        var step = new BaseStep
        {
            StepKind = "x",
            Description = "y",
            OnFail = new Branch
            {
                Decision = Decision.NextStep,
                WhenCode = ImmutableDictionary<int, Branch>.Empty.Add(5600, new Branch
                {
                    Decision = Decision.BreakWorkflow,
                    Description = "Boom"
                })
            }
        };

        var json = Serialize<WorkflowStep>(step);
        var back = Deserialize<WorkflowStep>(json);

        back.OnFail!.WhenCode!.Should().ContainKey(5600);
        back.OnFail.WhenCode[5600].Decision.Should().Be(Decision.BreakWorkflow);
        back.OnFail.WhenCode[5600].Description.Should().Be("Boom");
    }

    [Fact]
    public void Reads_breakpoint_flat_fields_into_config()
    {
        const string payload = """
            {
              "step": "x",
              "description": "y",
              "setBreakpoint": true,
              "restoreAtNextStep": true,
              "breakIteration": false,
              "breakPointTimeout": 30000
            }
            """;

        var step = Deserialize<WorkflowStep>(payload);

        step.Breakpoint.Should().NotBeNull();
        step.Breakpoint!.Set.Should().BeTrue();
        step.Breakpoint.RestoreAtNextStep.Should().BeTrue();
        step.Breakpoint.BreakIteration.Should().BeFalse();
        step.Breakpoint.TimeoutMs.Should().Be(30000);
    }

    [Fact]
    public void Writes_breakpoint_as_flat_fields()
    {
        var step = new BaseStep
        {
            StepKind = "x",
            Description = "y",
            Breakpoint = new BreakpointConfig
            {
                Set = true,
                RestoreAtNextStep = true,
                TimeoutMs = 1500
            }
        };

        var json = Serialize<WorkflowStep>(step);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("setBreakpoint").GetBoolean().Should().BeTrue();
        root.GetProperty("restoreAtNextStep").GetBoolean().Should().BeTrue();
        root.GetProperty("breakPointTimeout").GetInt32().Should().Be(1500);
        root.TryGetProperty("breakIteration", out _).Should().BeFalse(
            "BreakIteration is null, must not be serialized");
    }

    [Fact]
    public void Skips_breakpoint_entirely_when_not_set()
    {
        var step = new BaseStep
        {
            StepKind = "x",
            Description = "y",
            Breakpoint = new BreakpointConfig { Set = false, RestoreAtNextStep = true }
        };

        var json = Serialize<WorkflowStep>(step);
        using var doc = JsonDocument.Parse(json);

        doc.RootElement.TryGetProperty("setBreakpoint", out _).Should().BeFalse();
        doc.RootElement.TryGetProperty("restoreAtNextStep", out _).Should().BeFalse();
    }

    [Fact]
    public void Skips_id_property_when_step_id_is_null()
    {
        var step = new BaseStep { StepKind = "x", Description = "y" };

        var json = Serialize<WorkflowStep>(step);
        using var doc = JsonDocument.Parse(json);

        doc.RootElement.TryGetProperty("id", out _).Should().BeFalse();
    }

    [Fact]
    public void Skips_branch_properties_when_null()
    {
        var step = new BaseStep { StepKind = "x", Description = "y" };

        var json = Serialize<WorkflowStep>(step);
        using var doc = JsonDocument.Parse(json);

        doc.RootElement.TryGetProperty("onSuccess", out _).Should().BeFalse();
        doc.RootElement.TryGetProperty("onFail", out _).Should().BeFalse();
    }

    [Fact]
    public void Round_trip_full_conditional_step_preserves_all_fields()
    {
        var original = new BaseStep
        {
            StepKind = "check",
            StepId = "guard1",
            Description = "Check it",
            OnSuccess = new Branch { Decision = Decision.GotoStep, StepId = "next_one" },
            OnFail = new Branch
            {
                Decision = Decision.NextStep,
                WhenCode = ImmutableDictionary<int, Branch>.Empty.Add(5600, new Branch
                {
                    Decision = Decision.BreakWorkflow,
                    ErrorCode = 5600,
                    ErrorMessage = "Not found"
                })
            },
            Breakpoint = new BreakpointConfig
            {
                Set = true,
                RestoreAtNextStep = true,
                BreakIteration = true,
                TimeoutMs = 5000
            }
        };

        var json = Serialize<WorkflowStep>(original);
        var back = (BaseStep)Deserialize<WorkflowStep>(json);

        back.StepKind.Should().Be("check");
        back.StepId.Should().Be("guard1");
        back.OnSuccess!.Decision.Should().Be(Decision.GotoStep);
        back.OnSuccess.StepId.Should().Be("next_one");
        back.OnFail!.WhenCode![5600].ErrorCode.Should().Be(5600);
        back.OnFail.WhenCode[5600].ErrorMessage.Should().Be("Not found");
        back.Breakpoint!.Set.Should().BeTrue();
        back.Breakpoint.TimeoutMs.Should().Be(5000);
    }
}
