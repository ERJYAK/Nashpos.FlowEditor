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
            Iterate = step.Iterate ?? false,
            Id = step.StepId ?? string.Empty
        };

        if (step.Context is not null && !step.Context.IsEmpty)
        {
            dto.Context = ToProto(step.Context);
        }

        if (step.OnSuccess is not null) dto.OnSuccess = ToProto(step.OnSuccess);
        if (step.OnFail is not null)    dto.OnFail = ToProto(step.OnFail);
        if (step.Breakpoint is { Set: true } bp) dto.Breakpoint = ToProto(bp);

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
        var stepId = string.IsNullOrEmpty(dto.Id) ? null : dto.Id;
        var onSuccess = dto.OnSuccess is null ? null : FromProto(dto.OnSuccess);
        var onFail = dto.OnFail is null ? null : FromProto(dto.OnFail);
        var breakpoint = dto.Breakpoint is null ? null : FromProto(dto.Breakpoint);

        return dto.KindCase switch
        {
            Step.KindOneofCase.Subflow => new DomainSteps.SubflowStep
            {
                SubflowName = dto.Subflow.SubflowName,
                StepId = stepId,
                Description = description,
                Iterate = iterate,
                Context = context,
                OnSuccess = onSuccess,
                OnFail = onFail,
                Breakpoint = breakpoint
            },
            Step.KindOneofCase.Base => new DomainSteps.BaseStep
            {
                StepKind = dto.Base.StepKind,
                StepId = stepId,
                Description = description,
                Iterate = iterate,
                Context = context,
                OnSuccess = onSuccess,
                OnFail = onFail,
                Breakpoint = breakpoint
            },
            _ => throw new InvalidOperationException(
                "step has no kind set (forward-incompatible payload)")
        };
    }

    public static Branch ToProto(DomainModels.Branch src)
    {
        var dto = new Branch
        {
            Decision = (Decision)src.Decision,
            StepId = src.StepId ?? string.Empty,
            ErrorMessage = src.ErrorMessage ?? string.Empty,
            Description = src.Description ?? string.Empty
        };
        if (src.ErrorCode is { } code) dto.ErrorCode = code;
        if (src.WhenCode is not null)
        {
            foreach (var kv in src.WhenCode)
                dto.WhenCode.Add(kv.Key, ToProto(kv.Value));
        }
        return dto;
    }

    public static DomainModels.Branch FromProto(Branch dto)
    {
        ImmutableDictionary<int, DomainModels.Branch>? when = null;
        if (dto.WhenCode.Count > 0)
        {
            var b = ImmutableDictionary.CreateBuilder<int, DomainModels.Branch>();
            foreach (var kv in dto.WhenCode) b.Add(kv.Key, FromProto(kv.Value));
            when = b.ToImmutable();
        }

        return new DomainModels.Branch
        {
            Decision = (DomainModels.Decision)dto.Decision,
            StepId = string.IsNullOrEmpty(dto.StepId) ? null : dto.StepId,
            ErrorCode = dto.HasErrorCode ? dto.ErrorCode : null,
            ErrorMessage = string.IsNullOrEmpty(dto.ErrorMessage) ? null : dto.ErrorMessage,
            Description = string.IsNullOrEmpty(dto.Description) ? null : dto.Description,
            WhenCode = when
        };
    }

    public static BreakpointConfig ToProto(DomainModels.BreakpointConfig src)
    {
        var dto = new BreakpointConfig { Set = src.Set };
        if (src.RestoreAtNextStep is { } r) dto.RestoreAtNextStep = r;
        if (src.BreakIteration is { } bi) dto.BreakIteration = bi;
        if (src.TimeoutMs is { } t) dto.TimeoutMs = t;
        return dto;
    }

    public static DomainModels.BreakpointConfig FromProto(BreakpointConfig dto) =>
        new()
        {
            Set = dto.Set,
            RestoreAtNextStep = dto.HasRestoreAtNextStep ? dto.RestoreAtNextStep : null,
            BreakIteration = dto.HasBreakIteration ? dto.BreakIteration : null,
            TimeoutMs = dto.HasTimeoutMs ? dto.TimeoutMs : null
        };

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
