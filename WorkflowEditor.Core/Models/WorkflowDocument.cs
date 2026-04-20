namespace WorkflowEditor.Core.Models;

using System.Text.Json.Serialization;

public class WorkflowDocument
{
    [JsonPropertyName("workflowId")]
    public string WorkflowId { get; init; } = Guid.NewGuid().ToString();

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("steps")]
    public List<WorkflowStep> Steps { get; set; } = new();

    [JsonPropertyName("links")]
    public List<WorkflowLink> Links { get; set; } = new();
}