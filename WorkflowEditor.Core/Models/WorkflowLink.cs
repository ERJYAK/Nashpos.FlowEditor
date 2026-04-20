namespace WorkflowEditor.Core.Models;

using System.Text.Json.Serialization;

// Структура для координат узла на холсте
public record CanvasPosition(double X, double Y);

// Описание связи между узлами
public record WorkflowLink
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = Guid.NewGuid().ToString(); // init для неизменяемости после создания

    [JsonPropertyName("sourceNodeId")]
    public string SourceNodeId { get; set; } = string.Empty;

    [JsonPropertyName("sourcePortId")]
    public string SourcePortId { get; set; } = string.Empty;

    [JsonPropertyName("targetNodeId")]
    public string TargetNodeId { get; set; } = string.Empty;

    [JsonPropertyName("targetPortId")]
    public string TargetPortId { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string? Label { get; set; }
}