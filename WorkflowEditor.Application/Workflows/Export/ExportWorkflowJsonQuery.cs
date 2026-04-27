using System.Text.Json;
using WorkflowEditor.Application.Abstractions;
using WorkflowEditor.Application.Common;
using WorkflowEditor.Core.Serialization;

namespace WorkflowEditor.Application.Workflows.Export;

public sealed record ExportWorkflowJsonQuery(string Name);

public interface IExportWorkflowJsonQueryHandler
{
    Task<Result<string>> HandleAsync(ExportWorkflowJsonQuery query, CancellationToken ct);
}

public sealed class ExportWorkflowJsonQueryHandler(IWorkflowRepository repository)
    : IExportWorkflowJsonQueryHandler
{
    private static readonly JsonSerializerOptions JsonOptions = JsonConfiguration.GetOptions();

    public async Task<Result<string>> HandleAsync(ExportWorkflowJsonQuery query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query.Name))
            return Error.Validation("name is required",
                new Dictionary<string, string[]> { ["name"] = ["required"] });

        var document = await repository.GetAsync(query.Name, ct);
        if (document is null)
            return Error.NotFound($"workflow '{query.Name}' not found");

        return Result<string>.Success(JsonSerializer.Serialize(document, JsonOptions));
    }
}
