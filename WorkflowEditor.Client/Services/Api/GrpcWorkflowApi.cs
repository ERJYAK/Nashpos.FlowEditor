using Grpc.Core;
using WorkflowEditor.Contracts.Grpc;
using WorkflowEditor.Contracts.Mapping;
using WorkflowDocument = WorkflowEditor.Core.Models.WorkflowDocument;

namespace WorkflowEditor.Client.Services.Api;

public sealed class GrpcWorkflowApi : IWorkflowApi
{
    private readonly WorkflowStorage.WorkflowStorageClient _client;

    public GrpcWorkflowApi(WorkflowStorage.WorkflowStorageClient client)
    {
        _client = client;
    }

    public async Task<ApiResult<WorkflowDocument>> GetAsync(string workflowId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.GetWorkflowAsync(
                new GetWorkflowRequest { WorkflowId = workflowId },
                cancellationToken: cancellationToken);

            if (response.Document is null)
                return ApiResult<WorkflowDocument>.ServerError("Сервер вернул пустой документ");

            return ApiResult<WorkflowDocument>.Success(WorkflowProtoMapper.FromProto(response.Document));
        }
        catch (RpcException ex)
        {
            return MapRpcException<WorkflowDocument>(ex);
        }
    }

    public async Task<ApiResult<Unit>> SaveAsync(WorkflowDocument document, CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.SaveWorkflowAsync(
                new SaveWorkflowRequest { Document = WorkflowProtoMapper.ToProto(document) },
                cancellationToken: cancellationToken);

            return ApiResult<Unit>.Success(Unit.Value);
        }
        catch (RpcException ex)
        {
            return MapRpcException<Unit>(ex);
        }
    }

    private static ApiResult<T> MapRpcException<T>(RpcException ex) => ex.StatusCode switch
    {
        StatusCode.NotFound => ApiResult<T>.NotFound(),
        StatusCode.InvalidArgument or StatusCode.FailedPrecondition =>
            ApiResult<T>.Validation(ex.Status.Detail),
        StatusCode.Unavailable or StatusCode.DeadlineExceeded or StatusCode.Cancelled =>
            ApiResult<T>.Network(ex.Status.Detail),
        _ => ApiResult<T>.ServerError(ex.Status.Detail)
    };
}
