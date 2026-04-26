namespace WorkflowEditor.Core.Models;

using System.Text.Json.Serialization;

// Структура для координат узла на холсте
public readonly record struct CanvasPosition(double X, double Y);

// Описание связи между узлами
public record WorkflowLink
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = Guid.NewGuid().ToString(); // init для неизменяемости после создания

    [JsonPropertyName("sourceNodeId")]
    public string SourceNodeId { get; init; } = string.Empty;

    [JsonPropertyName("sourcePortId")]
    public string SourcePortId { get; init; } = string.Empty;

    [JsonPropertyName("targetNodeId")]
    public string TargetNodeId { get; init; } = string.Empty;

    [JsonPropertyName("targetPortId")]
    public string TargetPortId { get; init; } = string.Empty;

    [JsonPropertyName("label")]
    public string? Label { get; init; }
}