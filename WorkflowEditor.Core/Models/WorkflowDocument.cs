using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace WorkflowEditor.Core.Models;

// Бизнес-документ workflow. Соответствует JSON-файлу:
//   { "description": "...", "steps": [ { "step"|"subflow": "...", ... }, ... ] }
//
// `Name` — имя процесса = имя файла (без расширения) = первичный ключ хранения.
// В JSON-формате `Name` НЕ хранится: он определяется именем файла или диалогом «Создать процесс».
// На уровне доменной модели и API это имя — обязательно.
public sealed record WorkflowDocument
{
    [JsonIgnore]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("steps")]
    public ImmutableList<WorkflowStep> Steps { get; init; } = ImmutableList<WorkflowStep>.Empty;
}
