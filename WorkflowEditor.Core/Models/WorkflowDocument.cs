namespace WorkflowEditor.Core.Models;

using System.Text.Json.Serialization;

public record WorkflowDocument
{
    [JsonPropertyName("workflowId")]
    public string WorkflowId { get; init; } = Guid.NewGuid().ToString();

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("steps")]
    public List<WorkflowStep> Steps { get; init; } = new();

    [JsonPropertyName("links")]
    public List<WorkflowLink> Links { get; init; } = new();
    
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}