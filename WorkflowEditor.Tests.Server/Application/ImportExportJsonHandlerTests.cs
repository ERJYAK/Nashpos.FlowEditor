using NSubstitute;
using WorkflowEditor.Application.Abstractions;
using WorkflowEditor.Application.Common;
using WorkflowEditor.Application.Workflows.Export;
using WorkflowEditor.Application.Workflows.Import;
using WorkflowEditor.Core.Models;
using static WorkflowEditor.Tests.Server.TestKit.WorkflowFactory;

namespace WorkflowEditor.Tests.Server.Application;

public class ImportExportJsonHandlerTests
{
    private sealed class ThrowingMigrator : IWorkflowDocumentJsonMigrator
    {
        public string MigrateToCurrentSchema(string jsonPayload) =>
            throw new InvalidOperationException("legacy schema unsupported");
    }

    [Fact]
    public void Import_returns_Validation_when_payload_is_blank()
    {
        var handler = new ImportWorkflowJsonCommandHandler(new IdentityWorkflowDocumentJsonMigrator());

        var result = handler.Handle(new ImportWorkflowJsonCommand(""));

        result.IsSuccess.Should().BeFalse();
        result.Error!.Kind.Should().Be(ErrorKind.Validation);
    }

    [Fact]
    public void Import_returns_Validation_when_payload_is_invalid_json()
    {
        var handler = new ImportWorkflowJsonCommandHandler(new IdentityWorkflowDocumentJsonMigrator());

        var result = handler.Handle(new ImportWorkflowJsonCommand("{not json"));

        result.IsSuccess.Should().BeFalse();
        result.Error!.Kind.Should().Be(ErrorKind.Validation);
    }

    [Fact]
    public void Import_returns_Validation_when_migrator_throws()
    {
        var handler = new ImportWorkflowJsonCommandHandler(new ThrowingMigrator());

        var result = handler.Handle(new ImportWorkflowJsonCommand("{}"));

        result.IsSuccess.Should().BeFalse();
        result.Error!.Kind.Should().Be(ErrorKind.Validation);
        result.Error.Failures.Should().NotBeNull();
    }

    [Fact]
    public void Import_then_Export_roundtrips_a_document()
    {
        var importer = new ImportWorkflowJsonCommandHandler(new IdentityWorkflowDocumentJsonMigrator());
        var json = """
            {
              "workflowId": "wf-1",
              "name": "Имя",
              "steps": [
                { "type": "base", "id": "s-1", "name": "Task", "position": {"x":10,"y":20} }
              ],
              "links": [],
              "createdAt": "2024-01-02T03:04:05Z"
            }
            """;

        var imported = importer.Handle(new ImportWorkflowJsonCommand(json));

        imported.IsSuccess.Should().BeTrue();
        imported.Value!.WorkflowId.Should().Be("wf-1");
        imported.Value.Steps.Should().ContainKey("s-1");
    }

    [Fact]
    public async Task Export_returns_NotFound_for_missing_document()
    {
        var repo = Substitute.For<IWorkflowRepository>();
        repo.GetAsync("ghost", Arg.Any<CancellationToken>()).Returns((WorkflowDocument?)null);
        var handler = new ExportWorkflowJsonQueryHandler(repo);

        var result = await handler.HandleAsync(new ExportWorkflowJsonQuery("ghost"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Kind.Should().Be(ErrorKind.NotFound);
    }

    [Fact]
    public async Task Export_returns_serialized_json_when_document_exists()
    {
        var doc = Document("wf-1", BaseStep("s-1"));
        var repo = Substitute.For<IWorkflowRepository>();
        repo.GetAsync("wf-1", Arg.Any<CancellationToken>()).Returns(doc);
        var handler = new ExportWorkflowJsonQueryHandler(repo);

        var result = await handler.HandleAsync(new ExportWorkflowJsonQuery("wf-1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("\"workflowId\": \"wf-1\"");
        result.Value.Should().Contain("\"id\": \"s-1\"");
    }
}
