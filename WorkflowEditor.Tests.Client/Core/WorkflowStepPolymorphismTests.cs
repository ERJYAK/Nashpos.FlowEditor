using WorkflowEditor.Core.Models;
using WorkflowEditor.Core.Models.Steps;
using static WorkflowEditor.Tests.Client.TestKit.EditorTestData;

namespace WorkflowEditor.Tests.Client.Core;

public class WorkflowStepPolymorphismTests
{
    [Fact]
    public void WithName_on_BaseStep_returns_BaseStep_with_new_name()
    {
        var step = BaseStep("s-1", "old", x: 5, y: 6);

        var renamed = step.WithName("new");

        renamed.Should().BeOfType<BaseStep>();
        renamed.Name.Should().Be("new");
        renamed.Id.Should().Be("s-1");
        renamed.Position.Should().Be(new CanvasPosition(5, 6));
    }

    [Fact]
    public void WithName_on_SubflowStep_preserves_subflowId()
    {
        var step = SubflowStep("s-1", "old", subflowId: "sub-42", x: 1, y: 2);

        var renamed = step.WithName("new");

        var sub = renamed.Should().BeOfType<SubflowStep>().Subject;
        sub.Name.Should().Be("new");
        sub.SubflowId.Should().Be("sub-42");
        sub.Position.Should().Be(new CanvasPosition(1, 2));
    }

    [Fact]
    public void WithPosition_on_BaseStep_keeps_name_and_id()
    {
        var step = BaseStep("s-1", "task", x: 0, y: 0);

        var moved = step.WithPosition(new CanvasPosition(10, 20));

        moved.Should().BeOfType<BaseStep>();
        moved.Position.Should().Be(new CanvasPosition(10, 20));
        moved.Id.Should().Be("s-1");
        moved.Name.Should().Be("task");
    }

    [Fact]
    public void WithPosition_on_SubflowStep_preserves_subflowId()
    {
        var step = SubflowStep("s-1", "sub", subflowId: "sub-1");

        var moved = step.WithPosition(new CanvasPosition(7, 8));

        var sub = moved.Should().BeOfType<SubflowStep>().Subject;
        sub.SubflowId.Should().Be("sub-1");
        sub.Position.Should().Be(new CanvasPosition(7, 8));
    }

    [Fact]
    public void CloneWithId_on_BaseStep_changes_only_id()
    {
        var step = BaseStep("orig", "task", x: 5, y: 6);

        var cloned = step.CloneWithId("new-id");

        cloned.Should().BeOfType<BaseStep>();
        cloned.Id.Should().Be("new-id");
        cloned.Name.Should().Be("task");
        cloned.Position.Should().Be(new CanvasPosition(5, 6));
    }

    [Fact]
    public void CloneWithId_on_SubflowStep_preserves_subflowId()
    {
        var step = SubflowStep("orig", "sub", subflowId: "sub-7", x: 1, y: 1);

        var cloned = step.CloneWithId("new-id");

        var sub = cloned.Should().BeOfType<SubflowStep>().Subject;
        sub.Id.Should().Be("new-id");
        sub.Name.Should().Be("sub");
        sub.SubflowId.Should().Be("sub-7");
        sub.Position.Should().Be(new CanvasPosition(1, 1));
    }
}
