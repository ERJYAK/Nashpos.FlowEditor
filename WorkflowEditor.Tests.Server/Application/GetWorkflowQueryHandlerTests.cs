using NSubstitute;
using WorkflowEditor.Application.Abstractions;
using WorkflowEditor.Application.Common;
using WorkflowEditor.Application.Workflows.Get;
using WorkflowEditor.Tests.Server.TestKit;

namespace WorkflowEditor.Tests.Server.Application;

public class GetWorkflowQueryHandlerTests
{
    [Fact]
    public async Task Returns_NotFound_when_repository_has_no_match()
    {
        var repo = Substitute.For<IWorkflowRepository>();
        repo.GetAsync("missing", Arg.Any<CancellationToken>()).Returns((Core.Models.WorkflowDocument?)null);

        var handler = new GetWorkflowQueryHandler(repo);
        var result = await handler.HandleAsync(new GetWorkflowQuery("missing"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Kind.Should().Be(ErrorKind.NotFound);
    }

    [Fact]
    public async Task Returns_Validation_when_name_is_blank()
    {
        var handler = new GetWorkflowQueryHandler(Substitute.For<IWorkflowRepository>());

        var result = await handler.HandleAsync(new GetWorkflowQuery("  "), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Kind.Should().Be(ErrorKind.Validation);
    }

    [Fact]
    public async Task Returns_Success_with_document_from_repository()
    {
        var doc = WorkflowFactory.Document("import", "Import flow", WorkflowFactory.Base("apply-import"));
        var repo = Substitute.For<IWorkflowRepository>();
        repo.GetAsync("import", Arg.Any<CancellationToken>()).Returns(doc);

        var handler = new GetWorkflowQueryHandler(repo);
        var result = await handler.HandleAsync(new GetWorkflowQuery("import"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(doc);
    }
}
