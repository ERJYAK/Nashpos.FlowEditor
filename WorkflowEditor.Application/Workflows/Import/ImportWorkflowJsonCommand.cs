using System.Text.Json;
using WorkflowEditor.Application.Common;
using WorkflowEditor.Core.Models;
using WorkflowEditor.Core.Serialization;

namespace WorkflowEditor.Application.Workflows.Import;

public sealed record ImportWorkflowJsonCommand(string Payload);

public interface IImportWorkflowJsonCommandHandler
{
    Result<WorkflowDocument> Handle(ImportWorkflowJsonCommand command);
}

public sealed class ImportWorkflowJsonCommandHandler(IWorkflowDocumentJsonMigrator migrator)
    : IImportWorkflowJsonCommandHandler
{
    private static readonly JsonSerializerOptions JsonOptions = JsonConfiguration.GetOptions();

    public Result<WorkflowDocument> Handle(ImportWorkflowJsonCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Payload))
            return Error.Validation("payload is empty",
                new Dictionary<string, string[]> { ["payload"] = ["required"] });

        string migrated;
        try
        {
            migrated = migrator.MigrateToCurrentSchema(command.Payload);
        }
        catch (Exception ex)
        {
            return Error.Validation($"json migration failed: {ex.Message}",
                new Dictionary<string, string[]> { ["payload"] = [ex.Message] });
        }

        try
        {
            var document = JsonSerializer.Deserialize<WorkflowDocument>(migrated, JsonOptions);
            return document is null
                ? Error.Validation("payload deserialized to null",
                    new Dictionary<string, string[]> { ["payload"] = ["empty document"] })
                : Result<WorkflowDocument>.Success(document);
        }
        catch (JsonException ex)
        {
            return Error.Validation($"invalid json payload: {ex.Message}",
                new Dictionary<string, string[]> { ["payload"] = [ex.Message] });
        }
    }
}
