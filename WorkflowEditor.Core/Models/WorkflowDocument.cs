using System.Collections.Immutable;
using System.Text.Json.Serialization;
using WorkflowEditor.Core.Serialization;

namespace WorkflowEditor.Core.Models;

public record WorkflowDocument
{
    [JsonPropertyName("workflowId")]
    public string WorkflowId { get; init; } = Guid.NewGuid().ToString();

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("steps")]
    [JsonConverter(typeof(WorkflowStepDictionaryConverter))]
    public ImmutableDictionary<string, WorkflowStep> Steps { get; init; } =
        ImmutableDictionary<string, WorkflowStep>.Empty;

    [JsonPropertyName("links")]
    [JsonConverter(typeof(WorkflowLinkDictionaryConverter))]
    public ImmutableDictionary<string, WorkflowLink> Links { get; init; } =
        ImmutableDictionary<string, WorkflowLink>.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
