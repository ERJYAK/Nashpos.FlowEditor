namespace WorkflowEditor.Core.Models;

using System.Text.Json.Serialization;
using WorkflowEditor.Core.Models.Steps;

[JsonPolymorphic(
    TypeDiscriminatorPropertyName = "type",
    UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization)]
[JsonDerivedType(typeof(SubflowStep), typeDiscriminator: "subflow")]
[JsonDerivedType(typeof(BaseStep), typeDiscriminator: "base")]
// Сюда будем добавлять новые типы узлов
public abstract record WorkflowStep
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = Guid.NewGuid().ToString();

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("position")]
    public CanvasPosition Position { get; init; } = new(0, 0);

    public abstract WorkflowStep WithName(string name);

    public abstract WorkflowStep WithPosition(CanvasPosition position);

    public abstract WorkflowStep CloneWithId(string newId);
}