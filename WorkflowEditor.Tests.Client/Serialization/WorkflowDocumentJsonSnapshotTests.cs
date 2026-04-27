using System.Text.Json;
using VerifyXunit;
using WorkflowEditor.Core.Models;
using WorkflowEditor.Core.Models.Steps;
using WorkflowEditor.Core.Serialization;

namespace WorkflowEditor.Tests.Client.Serialization;

public class WorkflowDocumentJsonSnapshotTests
{
    [Fact]
    public Task Document_with_mixed_step_types_serializes_to_known_shape()
    {
        var document = new WorkflowDocument
        {
            Name = "import",
            Description = "Import flow",
            Steps =
            [
                new SubflowStep { Id = "1", SubflowName = "prepare-import", Description = "Prepare import subflow" },
                new SubflowStep
                {
                    Id = "2",
                    SubflowName = "iterate-tenants",
                    Description = "Iterate through the tenant list",
                    Iterate = true
                },
                new BaseStep { Id = "3", StepKind = "apply-import", Description = "Transfer imported data" }
            ]
        };

        var json = JsonSerializer.Serialize(document, JsonConfiguration.GetOptions());

        return Verifier.Verify(json, extension: "json");
    }
}
