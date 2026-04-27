using System.Text.Json;
using NSubstitute;
using WorkflowEditor.Application.Abstractions;
using WorkflowEditor.Application.Common;
using WorkflowEditor.Application.Workflows.Export;
using WorkflowEditor.Application.Workflows.Import;
using WorkflowEditor.Core.Models;
using WorkflowEditor.Core.Models.Steps;
using WorkflowEditor.Tests.Server.TestKit;

namespace WorkflowEditor.Tests.Server.Application;

public class ImportExportJsonHandlerTests
{
    private static IImportWorkflowJsonCommandHandler ImportHandler() =>
        new ImportWorkflowJsonCommandHandler(new IdentityWorkflowDocumentJsonMigrator());

    [Fact]
    public void Import_assigns_Name_from_command_not_from_payload()
    {
        const string payload = """
            {
              "description": "Import flow",
              "steps": [ { "step": "apply-import", "description": "Apply" } ]
            }
            """;

        var result = ImportHandler().Handle(new ImportWorkflowJsonCommand("import", payload));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("import");
        result.Value.Description.Should().Be("Import flow");
        result.Value.Steps.Should().ContainSingle()
            .Which.Should().BeOfType<BaseStep>().Which.StepKind.Should().Be("apply-import");
    }

    [Fact]
    public void Import_returns_Validation_on_blank_name()
    {
        var result = ImportHandler().Handle(new ImportWorkflowJsonCommand("", "{}"));

        result.IsSuccess.Should().BeFalse();
        result.Error!.Kind.Should().Be(ErrorKind.Validation);
    }

    [Fact]
    public void Import_returns_Validation_on_invalid_json()
    {
        var result = ImportHandler().Handle(new ImportWorkflowJsonCommand("name", "{ not-json"));

        result.IsSuccess.Should().BeFalse();
        result.Error!.Kind.Should().Be(ErrorKind.Validation);
    }

    [Fact]
    public async Task Export_returns_serialized_document_without_Name_field()
    {
        var doc = WorkflowFactory.Document("import", "Import flow", WorkflowFactory.Base("apply-import"));
        var repo = Substitute.For<IWorkflowRepository>();
        repo.GetAsync("import", Arg.Any<CancellationToken>()).Returns(doc);

        var result = await new ExportWorkflowJsonQueryHandler(repo)
            .HandleAsync(new ExportWorkflowJsonQuery("import"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var parsed = JsonDocument.Parse(result.Value!);
        parsed.RootElement.TryGetProperty("name", out _).Should().BeFalse(
            "Name is the storage key, not part of the file format");
        parsed.RootElement.GetProperty("description").GetString().Should().Be("Import flow");
    }

    [Fact]
    public async Task Export_returns_NotFound_when_workflow_does_not_exist()
    {
        var repo = Substitute.For<IWorkflowRepository>();
        repo.GetAsync("missing", Arg.Any<CancellationToken>()).Returns((WorkflowDocument?)null);

        var result = await new ExportWorkflowJsonQueryHandler(repo)
            .HandleAsync(new ExportWorkflowJsonQuery("missing"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Kind.Should().Be(ErrorKind.NotFound);
    }
}
