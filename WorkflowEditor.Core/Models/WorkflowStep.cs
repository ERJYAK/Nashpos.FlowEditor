using System.Text.Json.Serialization;

namespace WorkflowEditor.Core.Models;

// Шаг процесса. Полиморфизм — `BaseStep` (дискриминатор `step`) и `SubflowStep` (дискриминатор `subflow`).
// Сериализуется кастомным `WorkflowStepJsonConverter`: единый ключ-дискриминатор (`step` / `subflow`)
// определяет тип, остальные поля — общие.
//
// `Id` — синтетический клиентский идентификатор (нужен только для адресации DOM/связей в редакторе);
// в файле его нет, при импорте — пере-генерируется.
public abstract record WorkflowStep
{
    [JsonIgnore]
    public string Id { get; init; } = Guid.NewGuid().ToString();

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("iterate")]
    public bool? Iterate { get; init; }

    [JsonPropertyName("context")]
    public StepContext? Context { get; init; }

    public abstract WorkflowStep WithDescription(string description);

    public abstract WorkflowStep CloneAsNew();
}
