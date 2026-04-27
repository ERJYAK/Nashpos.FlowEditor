using System.Text.Json;
using Grpc.Core;
using WorkflowEditor.Api.Grpc;
using WorkflowEditor.Application.Workflows.Get;
using WorkflowEditor.Application.Workflows.Save;
using WorkflowEditor.Contracts.Grpc;
using WorkflowEditor.Core.Models;
using WorkflowEditor.Core.Serialization;

namespace WorkflowEditor.Api.Services;

public sealed class WorkflowStorageService(
    IGetWorkflowQueryHandler getHandler,
    ISaveWorkflowCommandHandler saveHandler,
    ILogger<WorkflowStorageService> logger) : WorkflowStorage.WorkflowStorageBase
{
    private static readonly JsonSerializerOptions JsonOptions = JsonConfiguration.GetOptions();

    public override async Task<WorkflowDocumentResponse> GetWorkflow(GetWorkflowRequest request, ServerCallContext context)
    {
        logger.LogInformation("get workflow {WorkflowId}", request.WorkflowId);

        var result = await getHandler.HandleAsync(new GetWorkflowQuery(request.WorkflowId), context.CancellationToken);
        if (!result.IsSuccess) throw result.Error!.ToRpcException();

        var document = result.Value!;
        return new WorkflowDocumentResponse
        {
            WorkflowId = document.WorkflowId,
            Name = document.Name,
            JsonPayload = JsonSerializer.Serialize(document, JsonOptions)
        };
    }

    public override async Task<SaveWorkflowResponse> SaveWorkflow(SaveWorkflowRequest request, ServerCallContext context)
    {
        logger.LogInformation("save workflow {WorkflowId} ({PayloadSize} chars)",
            request.WorkflowId, request.JsonPayload.Length);

        WorkflowDocument? document;
        try
        {
            document = JsonSerializer.Deserialize<WorkflowDocument>(request.JsonPayload, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"invalid JSON payload: {ex.Message}"));
        }

        if (document is null)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "json payload is empty"));

        if (!string.Equals(document.WorkflowId, request.WorkflowId, StringComparison.Ordinal))
            throw new RpcException(new Status(
                StatusCode.InvalidArgument,
                "workflowId in payload must match request.workflowId"));

        var result = await saveHandler.HandleAsync(new SaveWorkflowCommand(document), context.CancellationToken);
        if (!result.IsSuccess) throw result.Error!.ToRpcException();

        return new SaveWorkflowResponse { Success = true, ErrorMessage = string.Empty };
    }
}
