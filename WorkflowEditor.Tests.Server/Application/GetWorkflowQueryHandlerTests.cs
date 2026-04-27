using NSubstitute;
using WorkflowEditor.Application.Abstractions;
using WorkflowEditor.Application.Common;
using WorkflowEditor.Application.Workflows.Get;
using WorkflowEditor.Core.Models;
using static WorkflowEditor.Tests.Server.TestKit.WorkflowFactory;

namespace WorkflowEditor.Tests.Server.Application;

public class GetWorkflowQueryHandlerTests
{
    [Fact]
    public async Task returns_NotFound_when_repository_has_no_match()
    {
        var repo = Substitute.For<IWorkflowRepository>();
        repo.GetAsync("missing", Arg.Any<CancellationToken>()).Returns((WorkflowDocument?)null);
        var handler = new GetWorkflowQueryHandler(repo);

        var result = await handler.HandleAsync(new GetWorkflowQuery("missing"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Kind.Should().Be(ErrorKind.NotFound);
    }

    [Fact]
    public async Task returns_Validation_when_workflowId_is_empty()
    {
        var repo = Substitute.For<IWorkflowRepository>();
        var handler = new GetWorkflowQueryHandler(repo);

        var result = await handler.HandleAsync(new GetWorkflowQuery(""), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Kind.Should().Be(ErrorKind.Validation);
        await repo.DidNotReceive().GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task returns_document_on_success()
    {
        var doc = Document("wf-1", BaseStep("s-1"));
        var repo = Substitute.For<IWorkflowRepository>();
        repo.GetAsync("wf-1", Arg.Any<CancellationToken>()).Returns(doc);
        var handler = new GetWorkflowQueryHandler(repo);

        var result = await handler.HandleAsync(new GetWorkflowQuery("wf-1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(doc);
    }
}
