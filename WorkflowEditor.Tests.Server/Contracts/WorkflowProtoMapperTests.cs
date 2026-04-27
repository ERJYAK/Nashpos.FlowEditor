using System.Collections.Immutable;
using WorkflowEditor.Contracts.Mapping;
using WorkflowEditor.Core.Models;
using WorkflowEditor.Core.Models.Steps;
using ProtoStep = WorkflowEditor.Contracts.Grpc.Step;
using DomainDoc = WorkflowEditor.Core.Models.WorkflowDocument;

namespace WorkflowEditor.Tests.Server.Contracts;

public class WorkflowProtoMapperTests
{
    [Fact]
    public void Roundtrip_preserves_step_kinds_and_subflow_id()
    {
        var original = new DomainDoc
        {
            WorkflowId = "wf-1",
            Name = "doc",
            CreatedAt = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc),
            Steps = new WorkflowStep[]
            {
                new BaseStep { Id = "b", Name = "Base", Position = new CanvasPosition(10, 20) },
                new SubflowStep { Id = "s", Name = "Sub", SubflowId = "sub-7", Position = new CanvasPosition(30, 40) }
            }.ToImmutableDictionary(s => s.Id),
            Links = new[]
            {
                new WorkflowLink
                {
                    Id = "l", SourceNodeId = "b", SourcePortId = "Right",
                    TargetNodeId = "s", TargetPortId = "Left", Label = "next"
                }
            }.ToImmutableDictionary(l => l.Id)
        };

        var dto = WorkflowProtoMapper.ToProto(original);
        var restored = WorkflowProtoMapper.FromProto(dto);

        restored.WorkflowId.Should().Be("wf-1");
        restored.Name.Should().Be("doc");
        restored.CreatedAt.Should().Be(original.CreatedAt);

        restored.Steps["b"].Should().BeOfType<BaseStep>()
            .Which.Position.Should().Be(new CanvasPosition(10, 20));
        restored.Steps["s"].Should().BeOfType<SubflowStep>()
            .Which.SubflowId.Should().Be("sub-7");

        restored.Links["l"].Label.Should().Be("next");
        restored.Links["l"].SourceNodeId.Should().Be("b");
        restored.Links["l"].TargetNodeId.Should().Be("s");
    }

    [Fact]
    public void FromProto_throws_when_step_kind_is_missing()
    {
        var step = new ProtoStep
        {
            Id = "x",
            Name = "no kind"
        };

        Action act = () => WorkflowProtoMapper.FromProto(step);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*no kind set*");
    }

    [Fact]
    public void Empty_link_label_in_proto_maps_to_null_in_domain()
    {
        var dto = WorkflowProtoMapper.ToProto(new DomainDoc
        {
            WorkflowId = "wf-1",
            Name = "doc",
            Links = new[]
            {
                new WorkflowLink
                {
                    Id = "l", SourceNodeId = "a", TargetNodeId = "b",
                    SourcePortId = "Right", TargetPortId = "Left", Label = null
                }
            }.ToImmutableDictionary(l => l.Id)
        });

        var restored = WorkflowProtoMapper.FromProto(dto);
        restored.Links["l"].Label.Should().BeNull();
    }
}
