using NSubstitute;
using WorkflowEditor.Application.Abstractions;
using WorkflowEditor.Application.Common;
using WorkflowEditor.Application.Workflows.Save;
using WorkflowEditor.Core.Models;
using static WorkflowEditor.Tests.Server.TestKit.WorkflowFactory;

namespace WorkflowEditor.Tests.Server.Application;

public class SaveWorkflowCommandHandlerTests
{
    private static readonly string ValidWorkflowId = Guid.NewGuid().ToString();

    [Fact]
    public async Task returns_Validation_when_workflowId_is_not_a_guid()
    {
        var repo = Substitute.For<IWorkflowRepository>();
        var handler = new SaveWorkflowCommandHandler(repo, new SaveWorkflowValidator());
        var doc = Document("not-a-guid", BaseStep("s-1"));

        var result = await handler.HandleAsync(new SaveWorkflowCommand(doc), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Kind.Should().Be(ErrorKind.Validation);
        await repo.DidNotReceive().UpsertAsync(Arg.Any<WorkflowDocument>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task returns_Validation_when_step_ids_collide()
    {
        var repo = Substitute.For<IWorkflowRepository>();
        var handler = new SaveWorkflowCommandHandler(repo, new SaveWorkflowValidator());
        var doc = Document(ValidWorkflowId, BaseStep("dup"), BaseStep("dup"));

        var result = await handler.HandleAsync(new SaveWorkflowCommand(doc), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Kind.Should().Be(ErrorKind.Validation);
        result.Error.Failures.Should().NotBeNull();
    }

    [Fact]
    public async Task returns_Validation_when_link_references_unknown_step()
    {
        var repo = Substitute.For<IWorkflowRepository>();
        var handler = new SaveWorkflowCommandHandler(repo, new SaveWorkflowValidator());
        var doc = Document(ValidWorkflowId, BaseStep("s-1"))
            .WithLinks(Link("l-1", "s-1", "ghost"));

        var result = await handler.HandleAsync(new SaveWorkflowCommand(doc), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Kind.Should().Be(ErrorKind.Validation);
    }

    [Fact]
    public async Task forwards_to_repository_when_document_is_valid()
    {
        var repo = Substitute.For<IWorkflowRepository>();
        var handler = new SaveWorkflowCommandHandler(repo, new SaveWorkflowValidator());
        var doc = Document(ValidWorkflowId, BaseStep("s-1"), BaseStep("s-2"))
            .WithLinks(Link("l-1", "s-1", "s-2"));

        var result = await handler.HandleAsync(new SaveWorkflowCommand(doc), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await repo.Received(1).UpsertAsync(doc, Arg.Any<CancellationToken>());
    }
}
