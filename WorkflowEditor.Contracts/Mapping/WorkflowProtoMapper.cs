using System.Collections.Immutable;
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
            WorkflowId = document.WorkflowId,
            Name = document.Name,
            CreatedAt = Timestamp.FromDateTime(EnsureUtc(document.CreatedAt))
        };

        foreach (var step in document.Steps.Values)
        {
            dto.Steps.Add(ToProto(step));
        }
        foreach (var link in document.Links.Values)
        {
            dto.Links.Add(ToProto(link));
        }

        return dto;
    }

    public static DomainModels.WorkflowDocument FromProto(WorkflowDocument dto)
    {
        var stepsBuilder = ImmutableDictionary.CreateBuilder<string, DomainModels.WorkflowStep>();
        foreach (var protoStep in dto.Steps)
        {
            var step = FromProto(protoStep);
            stepsBuilder[step.Id] = step;
        }

        var linksBuilder = ImmutableDictionary.CreateBuilder<string, DomainModels.WorkflowLink>();
        foreach (var protoLink in dto.Links)
        {
            var link = FromProto(protoLink);
            linksBuilder[link.Id] = link;
        }

        return new DomainModels.WorkflowDocument
        {
            WorkflowId = dto.WorkflowId,
            Name = dto.Name,
            CreatedAt = dto.CreatedAt?.ToDateTime() ?? default,
            Steps = stepsBuilder.ToImmutable(),
            Links = linksBuilder.ToImmutable()
        };
    }

    public static Step ToProto(DomainModels.WorkflowStep step)
    {
        var dto = new Step
        {
            Id = step.Id,
            Name = step.Name,
            Position = new Position { X = step.Position.X, Y = step.Position.Y }
        };

        switch (step)
        {
            case DomainSteps.SubflowStep subflow:
                dto.Subflow = new SubflowStepData { SubflowId = subflow.SubflowId };
                break;
            case DomainSteps.BaseStep:
                dto.Base = new BaseStepData();
                break;
            default:
                throw new InvalidOperationException($"unknown step type {step.GetType().Name}");
        }

        return dto;
    }

    public static DomainModels.WorkflowStep FromProto(Step dto)
    {
        var position = dto.Position is null
            ? new DomainModels.CanvasPosition(0, 0)
            : new DomainModels.CanvasPosition(dto.Position.X, dto.Position.Y);

        return dto.KindCase switch
        {
            Step.KindOneofCase.Subflow => new DomainSteps.SubflowStep
            {
                Id = dto.Id,
                Name = dto.Name,
                Position = position,
                SubflowId = dto.Subflow.SubflowId
            },
            Step.KindOneofCase.Base => new DomainSteps.BaseStep
            {
                Id = dto.Id,
                Name = dto.Name,
                Position = position
            },
            _ => throw new InvalidOperationException(
                $"step '{dto.Id}' has no kind set (forward-incompatible payload)")
        };
    }

    public static Link ToProto(DomainModels.WorkflowLink link) => new()
    {
        Id = link.Id,
        SourceNodeId = link.SourceNodeId,
        SourcePortId = link.SourcePortId,
        TargetNodeId = link.TargetNodeId,
        TargetPortId = link.TargetPortId,
        Label = link.Label ?? string.Empty
    };

    public static DomainModels.WorkflowLink FromProto(Link dto) => new()
    {
        Id = dto.Id,
        SourceNodeId = dto.SourceNodeId,
        SourcePortId = dto.SourcePortId,
        TargetNodeId = dto.TargetNodeId,
        TargetPortId = dto.TargetPortId,
        Label = string.IsNullOrEmpty(dto.Label) ? null : dto.Label
    };

    private static DateTime EnsureUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
