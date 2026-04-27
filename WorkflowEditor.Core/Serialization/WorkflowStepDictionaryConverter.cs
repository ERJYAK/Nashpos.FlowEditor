using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using WorkflowEditor.Core.Models;

namespace WorkflowEditor.Core.Serialization;

// Сериализует словарь шагов как JSON-массив (контракт для совместимости со старыми flow).
// Сортировка по Id при записи — для детерминированного snapshot-вывода.
public sealed class WorkflowStepDictionaryConverter : JsonConverter<ImmutableDictionary<string, WorkflowStep>>
{
    public override ImmutableDictionary<string, WorkflowStep> Read(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var list = JsonSerializer.Deserialize<List<WorkflowStep>>(ref reader, options);
        if (list is null) return ImmutableDictionary<string, WorkflowStep>.Empty;

        var builder = ImmutableDictionary.CreateBuilder<string, WorkflowStep>();
        foreach (var step in list)
        {
            builder[step.Id] = step;
        }
        return builder.ToImmutable();
    }

    public override void Write(
        Utf8JsonWriter writer, ImmutableDictionary<string, WorkflowStep> value, JsonSerializerOptions options)
    {
        var ordered = value.Values.OrderBy(s => s.Id, StringComparer.Ordinal).ToList();
        JsonSerializer.Serialize(writer, ordered, options);
    }
}
