using System.Text.Json;
using WorkflowEditor.Core.Models;
using WorkflowEditor.Core.Models.Steps;
using WorkflowEditor.Core.Serialization;

namespace WorkflowEditor.Tests.Client.Serialization;

public class WorkflowDocumentJsonDeserializationTests
{
    private static readonly JsonSerializerOptions Options = JsonConfiguration.GetOptions();

    [Fact]
    public void Roundtrip_preserves_step_types_and_link_endpoints()
    {
        var original = new WorkflowDocument
        {
            WorkflowId = "wf-1",
            Name = "doc",
            Steps =
            {
                new BaseStep { Id = "b", Name = "B", Position = new CanvasPosition(1, 2) },
                new SubflowStep { Id = "s", Name = "S", SubflowId = "sub", Position = new CanvasPosition(3, 4) }
            },
            Links =
            {
                new WorkflowLink
                {
                    Id = "l", SourceNodeId = "b", SourcePortId = "Right",
                    TargetNodeId = "s", TargetPortId = "Left"
                }
            }
        };

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<WorkflowDocument>(json, Options)!;

        restored.Steps.Should().HaveCount(2);
        restored.Steps[0].Should().BeOfType<BaseStep>();
        restored.Steps[1].Should().BeOfType<SubflowStep>()
            .Which.SubflowId.Should().Be("sub");
        restored.Links.Should().ContainSingle()
            .Which.SourceNodeId.Should().Be("b");
    }

    [Fact]
    public void Unknown_step_type_discriminator_throws()
    {
        const string json = """
            {
              "workflowId": "wf-1",
              "name": "doc",
              "steps": [ { "type": "future_kind", "id": "x", "name": "X" } ],
              "links": []
            }
            """;

        Action act = () => JsonSerializer.Deserialize<WorkflowDocument>(json, Options);

        act.Should().Throw<JsonException>();
    }
}
