using NSubstitute;
using WorkflowEditor.Application.Abstractions;
using WorkflowEditor.Application.Common;
using WorkflowEditor.Application.Workflows.Delete;

namespace WorkflowEditor.Tests.Server.Application;

public class DeleteWorkflowCommandHandlerTests
{
    [Fact]
    public async Task Returns_Success_true_when_repository_deletes()
    {
        var repo = Substitute.For<IWorkflowRepository>();
        repo.DeleteAsync("import", Arg.Any<CancellationToken>()).Returns(true);

        var handler = new DeleteWorkflowCommandHandler(repo);
        var result = await handler.HandleAsync(new DeleteWorkflowCommand("import"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task Returns_NotFound_when_repository_returns_false()
    {
        var repo = Substitute.For<IWorkflowRepository>();
        repo.DeleteAsync("missing", Arg.Any<CancellationToken>()).Returns(false);

        var handler = new DeleteWorkflowCommandHandler(repo);
        var result = await handler.HandleAsync(new DeleteWorkflowCommand("missing"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Kind.Should().Be(ErrorKind.NotFound);
    }

    [Fact]
    public async Task Returns_Validation_when_name_is_blank()
    {
        var handler = new DeleteWorkflowCommandHandler(Substitute.For<IWorkflowRepository>());

        var result = await handler.HandleAsync(new DeleteWorkflowCommand(""), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Kind.Should().Be(ErrorKind.Validation);
    }
}
