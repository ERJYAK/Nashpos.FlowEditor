using WorkflowEditor.Core.Models;
using WorkflowEditor.Core.Models.Steps;

namespace WorkflowEditor.Tests.Client.Core;

public class WorkflowStepPolymorphismTests
{
    [Fact]
    public void WithDescription_on_BaseStep_keeps_kind_and_id()
    {
        var step = new BaseStep { Id = "s-1", StepKind = "download-package", Description = "old" };

        var updated = step.WithDescription("new");

        updated.Should().BeOfType<BaseStep>();
        updated.Description.Should().Be("new");
        updated.Id.Should().Be("s-1");
        ((BaseStep)updated).StepKind.Should().Be("download-package");
    }

    [Fact]
    public void WithDescription_on_SubflowStep_preserves_subflow_name()
    {
        var step = new SubflowStep { Id = "s-1", SubflowName = "prepare-import", Description = "old" };

        var updated = step.WithDescription("new");

        var sub = updated.Should().BeOfType<SubflowStep>().Subject;
        sub.Description.Should().Be("new");
        sub.SubflowName.Should().Be("prepare-import");
        sub.Id.Should().Be("s-1");
    }

    [Fact]
    public void CloneAsNew_on_BaseStep_changes_only_id()
    {
        var step = new BaseStep { Id = "orig", StepKind = "process-table", Description = "d" };

        var cloned = step.CloneAsNew();

        cloned.Should().BeOfType<BaseStep>();
        cloned.Id.Should().NotBe("orig").And.NotBeEmpty();
        cloned.Description.Should().Be("d");
        ((BaseStep)cloned).StepKind.Should().Be("process-table");
    }

    [Fact]
    public void CloneAsNew_on_SubflowStep_preserves_subflow_name_and_description()
    {
        var step = new SubflowStep { Id = "orig", SubflowName = "iterate-tenants", Description = "d" };

        var cloned = step.CloneAsNew();

        var sub = cloned.Should().BeOfType<SubflowStep>().Subject;
        sub.Id.Should().NotBe("orig").And.NotBeEmpty();
        sub.SubflowName.Should().Be("iterate-tenants");
        sub.Description.Should().Be("d");
    }

    [Fact]
    public void WithStepKind_returns_new_BaseStep_with_updated_kind()
    {
        var step = new BaseStep { Id = "s", StepKind = "old", Description = "d" };

        var updated = step.WithStepKind("new-kind");

        updated.StepKind.Should().Be("new-kind");
        updated.Description.Should().Be("d");
        updated.Id.Should().Be("s");
    }

    [Fact]
    public void WithSubflowName_returns_new_SubflowStep_with_updated_name()
    {
        var step = new SubflowStep { Id = "s", SubflowName = "old", Description = "d" };

        var updated = step.WithSubflowName("new-name");

        updated.SubflowName.Should().Be("new-name");
        updated.Description.Should().Be("d");
        updated.Id.Should().Be("s");
    }
}
