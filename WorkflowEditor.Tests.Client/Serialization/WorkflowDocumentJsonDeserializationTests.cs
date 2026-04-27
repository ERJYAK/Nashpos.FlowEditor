using System.Collections.Immutable;
using System.Text.Json;
using WorkflowEditor.Core.Models;
using WorkflowEditor.Core.Models.Steps;
using WorkflowEditor.Core.Serialization;

namespace WorkflowEditor.Tests.Client.Serialization;

public class WorkflowDocumentJsonDeserializationTests
{
    private static readonly JsonSerializerOptions Options = JsonConfiguration.GetOptions();

    [Fact]
    public void Discriminator_step_yields_BaseStep_with_kind_in_StepKind()
    {
        const string json = """
            {
              "description": "doc",
              "steps": [
                { "step": "download-package", "description": "d1" }
              ]
            }
            """;

        var doc = JsonSerializer.Deserialize<WorkflowDocument>(json, Options)!;

        doc.Steps.Should().ContainSingle();
        var step = doc.Steps[0].Should().BeOfType<BaseStep>().Subject;
        step.StepKind.Should().Be("download-package");
        step.Description.Should().Be("d1");
    }

    [Fact]
    public void Discriminator_subflow_yields_SubflowStep_with_name()
    {
        const string json = """
            {
              "description": "doc",
              "steps": [
                { "subflow": "prepare-import", "description": "d2", "iterate": true }
              ]
            }
            """;

        var doc = JsonSerializer.Deserialize<WorkflowDocument>(json, Options)!;

        var step = doc.Steps[0].Should().BeOfType<SubflowStep>().Subject;
        step.SubflowName.Should().Be("prepare-import");
        step.Description.Should().Be("d2");
        step.Iterate.Should().BeTrue();
    }

    [Fact]
    public void Both_step_and_subflow_in_one_object_throws()
    {
        const string json = """
            { "description": "x", "steps": [ { "step": "a", "subflow": "b" } ] }
            """;

        var act = () => JsonSerializer.Deserialize<WorkflowDocument>(json, Options);

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Neither_step_nor_subflow_throws()
    {
        const string json = """
            { "description": "x", "steps": [ { "description": "no-kind" } ] }
            """;

        var act = () => JsonSerializer.Deserialize<WorkflowDocument>(json, Options);

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Context_with_strings_integers_objects_roundtrips()
    {
        const string json = """
            {
              "description": "doc",
              "steps": [
                {
                  "step": "lookup-table",
                  "description": "Get product slot",
                  "context": {
                    "strings": { "table": "product_slot" },
                    "integers": { "limit": 10 },
                    "objects": { "where": { "tenant_id": "abc", "active": true } }
                  }
                }
              ]
            }
            """;

        var doc = JsonSerializer.Deserialize<WorkflowDocument>(json, Options)!;
        var ctx = doc.Steps[0].Context!;

        ctx.Strings!["table"].Should().Be("product_slot");
        ctx.Integers!["limit"].Should().Be(10);
        ctx.Objects!.Should().ContainKey("where");
    }

    [Fact]
    public void Roundtrip_preserves_step_types_and_order()
    {
        var original = new WorkflowDocument
        {
            Name = "import",
            Description = "Import flow",
            Steps =
            [
                new SubflowStep { Id = "1", SubflowName = "prepare-import", Description = "Prepare" },
                new SubflowStep { Id = "2", SubflowName = "iterate-tenants", Description = "Iterate", Iterate = true },
                new BaseStep    { Id = "3", StepKind = "apply-import",      Description = "Apply"  }
            ]
        };

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<WorkflowDocument>(json, Options)!;

        restored.Steps.Should().HaveCount(3);
        restored.Steps[0].Should().BeOfType<SubflowStep>().Which.SubflowName.Should().Be("prepare-import");
        restored.Steps[1].Should().BeOfType<SubflowStep>().Which.Iterate.Should().BeTrue();
        restored.Steps[2].Should().BeOfType<BaseStep>().Which.StepKind.Should().Be("apply-import");
    }
}
