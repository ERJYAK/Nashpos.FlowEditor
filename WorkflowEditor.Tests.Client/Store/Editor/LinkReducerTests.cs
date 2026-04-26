using WorkflowEditor.Client.Store.Editor;
using static WorkflowEditor.Tests.Client.TestKit.EditorTestData;

namespace WorkflowEditor.Tests.Client.Store.Editor;

public class LinkReducerTests
{
    [Fact]
    public void AddLink_appends_to_links()
    {
        var doc = Document("wf-1", BaseStep("s-1"), BaseStep("s-2"));
        var state = StateWith(doc);
        var link = Link("l-1", "s-1", "s-2");

        var next = EditorReducers.ReduceAddLinkAction(state, new AddLinkAction("wf-1", link));

        next.OpenDocuments["wf-1"].Links.Should().ContainSingle().Which.Id.Should().Be("l-1");
    }

    [Fact]
    public void AddLink_is_idempotent_for_same_id()
    {
        var doc = Document("wf-1", BaseStep("s-1"), BaseStep("s-2"))
            .WithLinks(Link("l-1", "s-1", "s-2"));
        var state = StateWith(doc);

        var next = EditorReducers.ReduceAddLinkAction(state,
            new AddLinkAction("wf-1", Link("l-1", "s-1", "s-2")));

        next.OpenDocuments["wf-1"].Links.Should().ContainSingle();
    }

    [Fact]
    public void RemoveLink_drops_only_target_link()
    {
        var doc = Document("wf-1", BaseStep("s-1"), BaseStep("s-2"))
            .WithLinks(Link("l-1", "s-1", "s-2"), Link("l-2", "s-2", "s-1"));
        var state = StateWith(doc);

        var next = EditorReducers.ReduceRemoveLinkAction(state, new RemoveLinkAction("wf-1", "l-1"));

        next.OpenDocuments["wf-1"].Links.Should().ContainSingle().Which.Id.Should().Be("l-2");
    }
}
