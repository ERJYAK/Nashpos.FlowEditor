using NSubstitute;
using WorkflowEditor.Application.Abstractions;
using WorkflowEditor.Application.Common;
using WorkflowEditor.Application.Workflows.Delete;

namespace WorkflowEditor.Tests.Server.Application;

public class DeleteWorkflowCommandHandlerTests
{
    [Fact]
    public async Task returns_NotFound_when_repository_reports_no_match()
    {
        var repo = Substitute.For<IWorkflowRepository>();
        repo.DeleteAsync("missing", Arg.Any<CancellationToken>()).Returns(false);
        var handler = new DeleteWorkflowCommandHandler(repo);

        var result = await handler.HandleAsync(new DeleteWorkflowCommand("missing"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Kind.Should().Be(ErrorKind.NotFound);
    }

    [Fact]
    public async Task returns_Success_when_deletion_happens()
    {
        var repo = Substitute.For<IWorkflowRepository>();
        repo.DeleteAsync("wf-1", Arg.Any<CancellationToken>()).Returns(true);
        var handler = new DeleteWorkflowCommandHandler(repo);

        var result = await handler.HandleAsync(new DeleteWorkflowCommand("wf-1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task returns_Validation_when_workflowId_is_blank()
    {
        var repo = Substitute.For<IWorkflowRepository>();
        var handler = new DeleteWorkflowCommandHandler(repo);

        var result = await handler.HandleAsync(new DeleteWorkflowCommand("   "), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Kind.Should().Be(ErrorKind.Validation);
        await repo.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
