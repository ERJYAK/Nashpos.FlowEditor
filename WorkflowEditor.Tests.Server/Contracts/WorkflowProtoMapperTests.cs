using System.Collections.Immutable;
using System.Text.Json;
using WorkflowEditor.Contracts.Mapping;
using WorkflowEditor.Core.Models;
using WorkflowEditor.Core.Models.Steps;
using DomainSteps = WorkflowEditor.Core.Models.Steps;

namespace WorkflowEditor.Tests.Server.Contracts;

public class WorkflowProtoMapperTests
{
    [Fact]
    public void Roundtrips_BaseStep_and_SubflowStep_with_iterate_and_description()
    {
        var doc = new WorkflowDocument
        {
            Name = "import",
            Description = "Import flow",
            Steps =
            [
                new DomainSteps.SubflowStep { SubflowName = "prepare-import", Description = "Prepare", Iterate = true },
                new DomainSteps.BaseStep    { StepKind = "apply-import",      Description = "Apply"  }
            ]
        };

        var proto = WorkflowProtoMapper.ToProto(doc);
        var back  = WorkflowProtoMapper.FromProto(proto);

        back.Name.Should().Be("import");
        back.Description.Should().Be("Import flow");
        back.Steps.Should().HaveCount(2);
        back.Steps[0].Should().BeOfType<SubflowStep>()
            .Which.Should().Match<SubflowStep>(s => s.SubflowName == "prepare-import" && s.Iterate == true);
        back.Steps[1].Should().BeOfType<BaseStep>()
            .Which.StepKind.Should().Be("apply-import");
    }

    [Fact]
    public void Roundtrips_StepContext_with_strings_integers_and_nested_objects()
    {
        var objectsJson = JsonDocument.Parse("""{ "where": { "tenant_id": "abc" }, "select": [ "slot_id" ] }""");
        var ctx = new StepContext
        {
            Strings  = ImmutableDictionary<string, string>.Empty.Add("table", "product_slot"),
            Integers = ImmutableDictionary<string, long>.Empty.Add("limit", 5L),
            Objects  = ImmutableDictionary<string, JsonElement>.Empty
                .Add("where", objectsJson.RootElement.GetProperty("where").Clone())
                .Add("select", objectsJson.RootElement.GetProperty("select").Clone())
        };
        var doc = new WorkflowDocument
        {
            Name = "lookup",
            Description = "",
            Steps = [new DomainSteps.BaseStep { StepKind = "lookup-table", Description = "L", Context = ctx }]
        };

        var back = WorkflowProtoMapper.FromProto(WorkflowProtoMapper.ToProto(doc));

        var roundCtx = back.Steps[0].Context!;
        roundCtx.Strings!["table"].Should().Be("product_slot");
        roundCtx.Integers!["limit"].Should().Be(5);
        roundCtx.Objects!["where"].GetProperty("tenant_id").GetString().Should().Be("abc");
        roundCtx.Objects["select"].EnumerateArray().Single().GetString().Should().Be("slot_id");
    }

    [Fact]
    public void Iterate_false_is_not_round_tripped_back_to_true()
    {
        var doc = new WorkflowDocument
        {
            Name = "x", Description = "",
            Steps = [new DomainSteps.SubflowStep { SubflowName = "a", Iterate = null }]
        };

        var back = WorkflowProtoMapper.FromProto(WorkflowProtoMapper.ToProto(doc));

        back.Steps[0].Iterate.Should().NotBe(true);
    }

    [Fact]
    public void Missing_kind_throws_with_explicit_message()
    {
        var protoStep = new WorkflowEditor.Contracts.Grpc.Step { Description = "no kind" };

        var act = () => WorkflowProtoMapper.FromProto(protoStep);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*forward-incompatible*");
    }
}
