using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using WorkflowEditor.Api.Grpc;
using WorkflowEditor.Application.Workflows.Delete;
using WorkflowEditor.Application.Workflows.Get;
using WorkflowEditor.Application.Workflows.List;
using WorkflowEditor.Application.Workflows.Save;
using WorkflowEditor.Contracts.Grpc;
using WorkflowEditor.Contracts.Mapping;

namespace WorkflowEditor.Api.Services;

public sealed class WorkflowStorageService(
    IGetWorkflowQueryHandler getHandler,
    ISaveWorkflowCommandHandler saveHandler,
    IListWorkflowsQueryHandler listHandler,
    IDeleteWorkflowCommandHandler deleteHandler,
    ILogger<WorkflowStorageService> logger) : WorkflowStorage.WorkflowStorageBase
{
    public override async Task<GetWorkflowResponse> GetWorkflow(GetWorkflowRequest request, ServerCallContext context)
    {
        logger.LogInformation("get workflow {WorkflowId}", request.WorkflowId);

        var result = await getHandler.HandleAsync(new GetWorkflowQuery(request.WorkflowId), context.CancellationToken);
        if (!result.IsSuccess) throw result.Error!.ToRpcException();

        return new GetWorkflowResponse { Document = WorkflowProtoMapper.ToProto(result.Value!) };
    }

    public override async Task<SaveWorkflowResponse> SaveWorkflow(SaveWorkflowRequest request, ServerCallContext context)
    {
        if (request.Document is null)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "document is required"));

        logger.LogInformation("save workflow {WorkflowId} ({StepCount} steps)",
            request.Document.WorkflowId, request.Document.Steps.Count);

        var document = WorkflowProtoMapper.FromProto(request.Document);
        var result = await saveHandler.HandleAsync(new SaveWorkflowCommand(document), context.CancellationToken);
        if (!result.IsSuccess) throw result.Error!.ToRpcException();

        return new SaveWorkflowResponse { Document = WorkflowProtoMapper.ToProto(result.Value!) };
    }

    public override async Task<ListWorkflowsResponse> ListWorkflows(ListWorkflowsRequest request, ServerCallContext context)
    {
        var result = await listHandler.HandleAsync(new ListWorkflowsQuery(), context.CancellationToken);
        if (!result.IsSuccess) throw result.Error!.ToRpcException();

        var response = new ListWorkflowsResponse();
        foreach (var summary in result.Value!)
        {
            response.Items.Add(new WorkflowSummary
            {
                WorkflowId = summary.WorkflowId,
                Name = summary.Name,
                CreatedAt = Timestamp.FromDateTime(EnsureUtc(summary.CreatedAt)),
                UpdatedAt = Timestamp.FromDateTime(EnsureUtc(summary.UpdatedAt))
            });
        }
        return response;
    }

    public override async Task<DeleteWorkflowResponse> DeleteWorkflow(DeleteWorkflowRequest request, ServerCallContext context)
    {
        var result = await deleteHandler.HandleAsync(new DeleteWorkflowCommand(request.WorkflowId), context.CancellationToken);
        if (!result.IsSuccess) throw result.Error!.ToRpcException();

        return new DeleteWorkflowResponse { Deleted = result.Value };
    }

    private static DateTime EnsureUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
