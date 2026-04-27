using WorkflowEditor.Client.Services.Layout;
using WorkflowEditor.Core.Models;
using WorkflowEditor.Tests.Client.TestKit;

namespace WorkflowEditor.Tests.Client.Layout;

public class LinearAutoLayoutTests
{
    [Fact]
    public void Empty_input_returns_empty_layout()
    {
        var positions = LinearAutoLayout.ForSteps([]);
        positions.Should().BeEmpty();
    }

    [Fact]
    public void Three_steps_get_positions_stride_120_top_to_bottom()
    {
        var s1 = EditorTestData.Base("a", id: "1");
        var s2 = EditorTestData.Base("b", id: "2");
        var s3 = EditorTestData.Base("c", id: "3");

        var positions = LinearAutoLayout.ForSteps([s1, s2, s3]);

        positions["1"].Should().Be(new CanvasPosition(0, 0));
        positions["2"].Should().Be(new CanvasPosition(0, 120));
        positions["3"].Should().Be(new CanvasPosition(0, 240));
    }
}
