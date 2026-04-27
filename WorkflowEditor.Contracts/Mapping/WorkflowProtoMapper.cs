using System.Collections.Immutable;
using System.Text.Json;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using WorkflowEditor.Contracts.Grpc;
using DomainModels = WorkflowEditor.Core.Models;
using DomainSteps = WorkflowEditor.Core.Models.Steps;

namespace WorkflowEditor.Contracts.Mapping;

public static class WorkflowProtoMapper
{
    public static WorkflowDocument ToProto(DomainModels.WorkflowDocument document)
    {
        var dto = new WorkflowDocument
        {
            Name = document.Name,
            Description = document.Description
        };

        foreach (var step in document.Steps)
        {
            dto.Steps.Add(ToProto(step));
        }
        return dto;
    }

    public static DomainModels.WorkflowDocument FromProto(WorkflowDocument dto)
    {
        var stepsBuilder = ImmutableList.CreateBuilder<DomainModels.WorkflowStep>();
        foreach (var protoStep in dto.Steps)
        {
            stepsBuilder.Add(FromProto(protoStep));
        }

        return new DomainModels.WorkflowDocument
        {
            Name = dto.Name,
            Description = dto.Description,
            Steps = stepsBuilder.ToImmutable()
        };
    }

    public static Step ToProto(DomainModels.WorkflowStep step)
    {
        var dto = new Step
        {
            Description = step.Description,
            Iterate = step.Iterate ?? false
        };

        if (step.Context is not null && !step.Context.IsEmpty)
        {
            dto.Context = ToProto(step.Context);
        }

        switch (step)
        {
            case DomainSteps.SubflowStep subflow:
                dto.Subflow = new SubflowStepData { SubflowName = subflow.SubflowName };
                break;
            case DomainSteps.BaseStep baseStep:
                dto.Base = new BaseStepData { StepKind = baseStep.StepKind };
                break;
            default:
                throw new InvalidOperationException($"unknown step type {step.GetType().Name}");
        }

        return dto;
    }

    public static DomainModels.WorkflowStep FromProto(Step dto)
    {
        var description = dto.Description;
        bool? iterate = dto.Iterate ? true : null;
        var context = dto.Context is null ? null : FromProto(dto.Context);

        return dto.KindCase switch
        {
            Step.KindOneofCase.Subflow => new DomainSteps.SubflowStep
            {
                SubflowName = dto.Subflow.SubflowName,
                Description = description,
                Iterate = iterate,
                Context = context
            },
            Step.KindOneofCase.Base => new DomainSteps.BaseStep
            {
                StepKind = dto.Base.StepKind,
                Description = description,
                Iterate = iterate,
                Context = context
            },
            _ => throw new InvalidOperationException(
                "step has no kind set (forward-incompatible payload)")
        };
    }

    public static StepContext ToProto(DomainModels.StepContext src)
    {
        var dto = new StepContext();
        if (src.Strings is not null)
        {
            foreach (var kv in src.Strings) dto.Strings.Add(kv.Key, kv.Value);
        }
        if (src.Integers is not null)
        {
            foreach (var kv in src.Integers) dto.Integers.Add(kv.Key, kv.Value);
        }
        if (src.Objects is not null)
        {
            foreach (var kv in src.Objects) dto.Objects.Add(kv.Key, JsonElementToValue(kv.Value));
        }
        return dto;
    }

    public static DomainModels.StepContext FromProto(StepContext dto)
    {
        var ctx = new DomainModels.StepContext
        {
            Strings = dto.Strings.Count == 0 ? null : dto.Strings.ToImmutableDictionary(),
            Integers = dto.Integers.Count == 0 ? null : dto.Integers.ToImmutableDictionary(),
            Objects = dto.Objects.Count == 0
                ? null
                : dto.Objects.ToImmutableDictionary(kv => kv.Key, kv => ValueToJsonElement(kv.Value))
        };
        return ctx;
    }

    private static Value JsonElementToValue(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.Null => Value.ForNull(),
        JsonValueKind.True => Value.ForBool(true),
        JsonValueKind.False => Value.ForBool(false),
        JsonValueKind.String => Value.ForString(el.GetString() ?? string.Empty),
        JsonValueKind.Number => Value.ForNumber(el.GetDouble()),
        JsonValueKind.Array => Value.ForList(el.EnumerateArray().Select(JsonElementToValue).ToArray()),
        JsonValueKind.Object => Value.ForStruct(new Struct
        {
            Fields = { el.EnumerateObject().ToDictionary(p => p.Name, p => JsonElementToValue(p.Value)) }
        }),
        _ => throw new InvalidOperationException($"unsupported JSON kind {el.ValueKind}")
    };

    private static JsonElement ValueToJsonElement(Value v)
    {
        var json = JsonFormatter.Default.Format(v);
        return JsonDocument.Parse(json).RootElement.Clone();
    }
}
