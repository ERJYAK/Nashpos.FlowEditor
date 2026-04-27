using System.Text.Json.Serialization;

namespace WorkflowEditor.Core.Models.Steps;

// Узел-ссылка на другой холст (твой кастомный subflow)
public record SubflowStep : WorkflowStep
{
    [JsonPropertyName("subflowId")]
    public string SubflowId { get; init; } = string.Empty;

    // В UI при двойном клике по этому узлу, мы будем брать SubflowId
    // и открывать новую вкладку, запрашивая по gRPC нужный документ.

    public override WorkflowStep WithName(string name) => this with { Name = name };

    public override WorkflowStep WithPosition(CanvasPosition position) => this with { Position = position };

    public override WorkflowStep CloneWithId(string newId) => this with { Id = newId };
}
