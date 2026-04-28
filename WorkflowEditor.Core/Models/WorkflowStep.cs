using System.Text.Json.Serialization;

namespace WorkflowEditor.Core.Models;

// Шаг процесса. Полиморфизм — `BaseStep` (дискриминатор `step`) и `SubflowStep` (дискриминатор `subflow`).
// Сериализуется кастомным `WorkflowStepJsonConverter`: единый ключ-дискриминатор (`step` / `subflow`)
// определяет тип, остальные поля — общие.
//
// `Id` — синтетический клиентский идентификатор (нужен только для адресации DOM/связей в редакторе);
// в файле его нет, при импорте — пере-генерируется.
//
// `StepId` — *persistent* идентификатор, попадающий в JSON под ключом `id`. Заполняется только тогда,
// когда на шаг ссылаются GOTO_STEP-переходы. Внутренний UI-Guid `Id` остаётся независимо.
public abstract record WorkflowStep
{
    [JsonIgnore]
    public string Id { get; init; } = Guid.NewGuid().ToString();

    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StepId { get; init; }

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("iterate")]
    public bool? Iterate { get; init; }

    [JsonPropertyName("context")]
    public StepContext? Context { get; init; }

    // Ветвление по результату шага. NULL ⇒ ветка не задана (по умолчанию NEXT_STEP).
    [JsonPropertyName("onSuccess")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Branch? OnSuccess { get; init; }

    [JsonPropertyName("onFail")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Branch? OnFail { get; init; }

    // Брейкпоинт. В JSON разворачивается в плоские поля (см. WorkflowStepJsonConverter).
    [JsonIgnore]
    public BreakpointConfig? Breakpoint { get; init; }

    public abstract WorkflowStep WithDescription(string description);

    public abstract WorkflowStep CloneAsNew();
}
