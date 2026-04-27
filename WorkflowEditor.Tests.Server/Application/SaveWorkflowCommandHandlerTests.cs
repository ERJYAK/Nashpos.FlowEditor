using FluentValidation;
using NSubstitute;
using WorkflowEditor.Application.Abstractions;
using WorkflowEditor.Application.Common;
using WorkflowEditor.Application.Workflows.Save;
using WorkflowEditor.Tests.Server.TestKit;

namespace WorkflowEditor.Tests.Server.Application;

public class SaveWorkflowCommandHandlerTests
{
    private static SaveWorkflowCommandHandler MakeHandler(out IWorkflowRepository repo)
    {
        repo = Substitute.For<IWorkflowRepository>();
        var validator = new SaveWorkflowValidator();
        return new SaveWorkflowCommandHandler(repo, validator);
    }

    [Fact]
    public async Task Persists_valid_document_via_repository_and_returns_it()
    {
        var handler = MakeHandler(out var repo);
        var doc = WorkflowFactory.Document("import", "Import flow", WorkflowFactory.Base("apply-import"));

        var result = await handler.HandleAsync(new SaveWorkflowCommand(doc), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(doc);
        await repo.Received(1).UpsertAsync(doc, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejects_invalid_name()
    {
        var handler = MakeHandler(out _);
        var doc = WorkflowFactory.Document("Bad Name!", steps: WorkflowFactory.Base("k"));

        var result = await handler.HandleAsync(new SaveWorkflowCommand(doc), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Kind.Should().Be(ErrorKind.Validation);
        result.Error.Failures.Should().ContainKey("Document.Name");
    }

    [Fact]
    public async Task Rejects_BaseStep_with_empty_StepKind()
    {
        var handler = MakeHandler(out _);
        var doc = WorkflowFactory.Document("import", steps: WorkflowFactory.Base(""));

        var result = await handler.HandleAsync(new SaveWorkflowCommand(doc), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Kind.Should().Be(ErrorKind.Validation);
    }

    [Fact]
    public async Task Rejects_SubflowStep_with_empty_SubflowName()
    {
        var handler = MakeHandler(out _);
        var doc = WorkflowFactory.Document("import", steps: WorkflowFactory.Sub(""));

        var result = await handler.HandleAsync(new SaveWorkflowCommand(doc), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Kind.Should().Be(ErrorKind.Validation);
    }
}
