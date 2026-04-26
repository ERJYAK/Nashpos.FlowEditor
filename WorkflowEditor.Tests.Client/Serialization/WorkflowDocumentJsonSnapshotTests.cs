using System.Text.Json;
using VerifyXunit;
using WorkflowEditor.Core.Models;
using WorkflowEditor.Core.Models.Steps;
using WorkflowEditor.Core.Serialization;

namespace WorkflowEditor.Tests.Client.Serialization;

public class WorkflowDocumentJsonSnapshotTests
{
    [Fact]
    public Task Document_with_mixed_step_types_and_link_serializes_to_known_shape()
    {
        var document = new WorkflowDocument
        {
            WorkflowId = "wf-snap",
            Name = "snapshot",
            CreatedAt = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc),
            Steps =
            {
                new BaseStep
                {
                    Id = "step-base",
                    Name = "Base",
                    Position = new CanvasPosition(10, 20)
                },
                new SubflowStep
                {
                    Id = "step-sub",
                    Name = "Subflow",
                    Position = new CanvasPosition(100, 200),
                    SubflowId = "sub-1"
                }
            },
            Links =
            {
                new WorkflowLink
                {
                    Id = "link-1",
                    SourceNodeId = "step-base",
                    SourcePortId = "Right",
                    TargetNodeId = "step-sub",
                    TargetPortId = "Left",
                    Label = "next"
                }
            }
        };

        var json = JsonSerializer.Serialize(document, JsonConfiguration.GetOptions());

        return Verifier.Verify(json, extension: "json");
    }
}
