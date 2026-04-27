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
    public void Roundtrip_preserves_step_types_and_link_endpoints()
    {
        var original = new WorkflowDocument
        {
            WorkflowId = "wf-1",
            Name = "doc",
            Steps = new WorkflowStep[]
            {
                new BaseStep { Id = "b", Name = "B", Position = new CanvasPosition(1, 2) },
                new SubflowStep { Id = "s", Name = "S", SubflowId = "sub", Position = new CanvasPosition(3, 4) }
            }.ToImmutableDictionary(s => s.Id),
            Links = new[]
            {
                new WorkflowLink
                {
                    Id = "l", SourceNodeId = "b", SourcePortId = "Right",
                    TargetNodeId = "s", TargetPortId = "Left"
                }
            }.ToImmutableDictionary(l => l.Id)
        };

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<WorkflowDocument>(json, Options)!;

        restored.Steps.Should().HaveCount(2);
        restored.Steps["b"].Should().BeOfType<BaseStep>();
        restored.Steps["s"].Should().BeOfType<SubflowStep>()
            .Which.SubflowId.Should().Be("sub");
        restored.Links.Should().ContainSingle()
            .Which.Value.SourceNodeId.Should().Be("b");
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
