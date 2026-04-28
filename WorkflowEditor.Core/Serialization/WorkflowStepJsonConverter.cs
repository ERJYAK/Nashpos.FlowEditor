using System.Text.Json;
using System.Text.Json.Serialization;
using WorkflowEditor.Core.Models;
using WorkflowEditor.Core.Models.Steps;

namespace WorkflowEditor.Core.Serialization;

// Дискриминатор шага зависит от **наличия ключа**, а не от значения общего поля
// (`{ "step": "<kind>" }` vs `{ "subflow": "<name>" }`). Стандартный `[JsonPolymorphic]`
// этого не умеет — пишем кастомный конвертер.
//
// Поля брейкпоинта (`setBreakpoint`, `restoreAtNextStep`, `breakIteration`, `breakPointTimeout`)
// в JSON лежат плоско на уровне шага, а в C# собираются в `BreakpointConfig`.
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

        var stepId = root.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.String
            ? idEl.GetString()
            : null;

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

        Branch? onSuccess = null;
        if (root.TryGetProperty("onSuccess", out var os) && os.ValueKind == JsonValueKind.Object)
            onSuccess = os.Deserialize<Branch>(options);

        Branch? onFail = null;
        if (root.TryGetProperty("onFail", out var of) && of.ValueKind == JsonValueKind.Object)
            onFail = of.Deserialize<Branch>(options);

        var breakpoint = ReadBreakpoint(root);

        if (hasStep)
        {
            return new BaseStep
            {
                StepKind = stepEl.ValueKind == JsonValueKind.String ? stepEl.GetString() ?? string.Empty : string.Empty,
                StepId = stepId,
                Description = description,
                Iterate = iterate,
                Context = context,
                OnSuccess = onSuccess,
                OnFail = onFail,
                Breakpoint = breakpoint
            };
        }

        return new SubflowStep
        {
            SubflowName = subflowEl.ValueKind == JsonValueKind.String ? subflowEl.GetString() ?? string.Empty : string.Empty,
            StepId = stepId,
            Description = description,
            Iterate = iterate,
            Context = context,
            OnSuccess = onSuccess,
            OnFail = onFail,
            Breakpoint = breakpoint
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

        if (!string.IsNullOrEmpty(value.StepId))
            writer.WriteString("id", value.StepId);

        writer.WriteString("description", value.Description);

        if (value.Iterate == true)
            writer.WriteBoolean("iterate", true);

        if (value.Context is not null && !value.Context.IsEmpty)
        {
            writer.WritePropertyName("context");
            JsonSerializer.Serialize(writer, value.Context, options);
        }

        if (value.OnSuccess is not null)
        {
            writer.WritePropertyName("onSuccess");
            JsonSerializer.Serialize(writer, value.OnSuccess, options);
        }

        if (value.OnFail is not null)
        {
            writer.WritePropertyName("onFail");
            JsonSerializer.Serialize(writer, value.OnFail, options);
        }

        WriteBreakpoint(writer, value.Breakpoint);

        writer.WriteEndObject();
    }

    private static BreakpointConfig? ReadBreakpoint(JsonElement root)
    {
        var hasSet = root.TryGetProperty("setBreakpoint", out var setEl)
                     && (setEl.ValueKind == JsonValueKind.True || setEl.ValueKind == JsonValueKind.False);
        var hasRestore = root.TryGetProperty("restoreAtNextStep", out var restoreEl)
                         && (restoreEl.ValueKind == JsonValueKind.True || restoreEl.ValueKind == JsonValueKind.False);
        var hasBreakIter = root.TryGetProperty("breakIteration", out var breakIterEl)
                           && (breakIterEl.ValueKind == JsonValueKind.True || breakIterEl.ValueKind == JsonValueKind.False);
        var hasTimeout = root.TryGetProperty("breakPointTimeout", out var timeoutEl)
                         && timeoutEl.ValueKind == JsonValueKind.Number;

        if (!hasSet && !hasRestore && !hasBreakIter && !hasTimeout)
            return null;

        return new BreakpointConfig
        {
            Set = hasSet && setEl.GetBoolean(),
            RestoreAtNextStep = hasRestore ? restoreEl.GetBoolean() : null,
            BreakIteration = hasBreakIter ? breakIterEl.GetBoolean() : null,
            TimeoutMs = hasTimeout ? timeoutEl.GetInt32() : null
        };
    }

    private static void WriteBreakpoint(Utf8JsonWriter writer, BreakpointConfig? bp)
    {
        if (bp is null || !bp.Set) return;

        writer.WriteBoolean("setBreakpoint", true);

        if (bp.RestoreAtNextStep is { } restore)
            writer.WriteBoolean("restoreAtNextStep", restore);
        if (bp.BreakIteration is { } breakIter)
            writer.WriteBoolean("breakIteration", breakIter);
        if (bp.TimeoutMs is { } timeout)
            writer.WriteNumber("breakPointTimeout", timeout);
    }
}
