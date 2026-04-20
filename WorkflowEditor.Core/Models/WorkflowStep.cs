namespace WorkflowEditor.Core.Models;

using System.Text.Json.Serialization;
using WorkflowEditor.Core.Models.Steps;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(SubflowStep), typeDiscriminator: "subflow")]
// Сюда будем добавлять новые типы узлов
public abstract class WorkflowStep
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = Guid.NewGuid().ToString();

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("position")]
    public CanvasPosition Position { get; set; } = new(0, 0);
}