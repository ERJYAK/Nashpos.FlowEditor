using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace WorkflowEditor.Core.Models;

// Маршрут шага. В JSON — UPPER_SNAKE_CASE.
[JsonConverter(typeof(JsonStringEnumConverter<Decision>))]
public enum Decision
{
    [JsonStringEnumMemberName("NEXT_STEP")]
    NextStep,
    [JsonStringEnumMemberName("GOTO_STEP")]
    GotoStep,
    [JsonStringEnumMemberName("BREAK_WORKFLOW")]
    BreakWorkflow,
    [JsonStringEnumMemberName("SILENT_BREAK_WORKFLOW")]
    SilentBreakWorkflow
}

// Ветка шага (`onSuccess` / `onFail` / запись в `whenCode`).
// Структура зеркалит JSON: decision + опциональные зависимые поля + рекурсивный whenCode.
public sealed record Branch
{
    [JsonPropertyName("decision")]
    public Decision Decision { get; init; } = Decision.NextStep;

    // Только для GOTO_STEP — id целевого шага.
    [JsonPropertyName("stepId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StepId { get; init; }

    // Только для BREAK_WORKFLOW.
    [JsonPropertyName("errorCode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ErrorCode { get; init; }

    [JsonPropertyName("errorMessage")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ErrorMessage { get; init; }

    // Только в onFail. Ключ — errorCode, значение — рекурсивный Branch.
    [JsonPropertyName("whenCode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ImmutableDictionary<int, Branch>? WhenCode { get; init; }

    // Описание записи whenCode (поле `description` внутри branch'а в JSON-примере ТЗ).
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; init; }

    [JsonIgnore]
    public bool IsTrivial =>
        Decision == Decision.NextStep
        && StepId is null
        && ErrorCode is null
        && string.IsNullOrEmpty(ErrorMessage)
        && string.IsNullOrEmpty(Description)
        && (WhenCode is null || WhenCode.Count == 0);
}
