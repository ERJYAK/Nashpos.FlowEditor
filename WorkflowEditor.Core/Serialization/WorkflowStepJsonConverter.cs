using System.Text.Json;
using System.Text.Json.Serialization;
using WorkflowEditor.Core.Models;
using WorkflowEditor.Core.Models.Steps;

namespace WorkflowEditor.Core.Serialization;

// Дискриминатор шага зависит от **наличия ключа**, а не от значения общего поля
// (`{ "step": "<kind>" }` vs `{ "subflow": "<name>" }`). Стандартный `[JsonPolymorphic]`
// этого не умеет — пишем кастомный конвертер.
public sealed class WorkflowStepJsonConverter : JsonConverter<WorkflowStep>
{
    public override WorkflowStep Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
            throw new JsonException("workflow step must be a JSON object");

        var hasStep = root.TryGetProperty("step", out var stepEl);
        var hasSubflow = root.TryGetProperty("subflow", out var subflowEl);

        if (hasStep && hasSubflow)
            throw new JsonException("workflow step cannot have both 'step' and 'subflow' discriminators");
        if (!hasStep && !hasSubflow)
            throw new JsonException("workflow step must have either 'step' or 'subflow' discriminator");

        var description = root.TryGetProperty("description", out var d) && d.ValueKind == JsonValueKind.String
            ? d.GetString() ?? string.Empty
            : string.Empty;

        bool? iterate = null;
        if (root.TryGetProperty("iterate", out var i) &&
            (i.ValueKind == JsonValueKind.True || i.ValueKind == JsonValueKind.False))
        {
            iterate = i.GetBoolean();
        }

        StepContext? context = null;
        if (root.TryGetProperty("context", out var c) && c.ValueKind == JsonValueKind.Object)
        {
            context = c.Deserialize<StepContext>(options);
            if (context is not null && context.IsEmpty) context = null;
        }

        if (hasStep)
        {
            return new BaseStep
            {
                StepKind = stepEl.ValueKind == JsonValueKind.String ? stepEl.GetString() ?? string.Empty : string.Empty,
                Description = description,
                Iterate = iterate,
                Context = context
            };
        }

        return new SubflowStep
        {
            SubflowName = subflowEl.ValueKind == JsonValueKind.String ? subflowEl.GetString() ?? string.Empty : string.Empty,
            Description = description,
            Iterate = iterate,
            Context = context
        };
    }

    public override void Write(Utf8JsonWriter writer, WorkflowStep value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        switch (value)
        {
            case BaseStep b:
                writer.WriteString("step", b.StepKind);
                break;
            case SubflowStep s:
                writer.WriteString("subflow", s.SubflowName);
                break;
            default:
                throw new JsonException($"unsupported workflow step type '{value.GetType().Name}'");
        }

        writer.WriteString("description", value.Description);

        if (value.Iterate == true)
            writer.WriteBoolean("iterate", true);

        if (value.Context is not null && !value.Context.IsEmpty)
        {
            writer.WritePropertyName("context");
            JsonSerializer.Serialize(writer, value.Context, options);
        }

        writer.WriteEndObject();
    }
}
