using System.Text.Json;
using Grpc.Core;
using WorkflowEditor.Contracts.Grpc;
using WorkflowEditor.Core.Models;
using WorkflowEditor.Core.Serialization;

namespace WorkflowEditor.Client.Services.Api;

public sealed class GrpcWorkflowApi : IWorkflowApi
{
    private static readonly JsonSerializerOptions JsonOptions = JsonConfiguration.GetOptions();

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

            var document = JsonSerializer.Deserialize<WorkflowDocument>(response.JsonPayload, JsonOptions);
            if (document is null)
                return ApiResult<WorkflowDocument>.ServerError("Сервер вернул пустой документ");

            return ApiResult<WorkflowDocument>.Success(document);
        }
        catch (RpcException ex)
        {
            return MapRpcException<WorkflowDocument>(ex);
        }
        catch (JsonException ex)
        {
            return ApiResult<WorkflowDocument>.ServerError($"Не удалось разобрать документ: {ex.Message}");
        }
    }

    public async Task<ApiResult<Unit>> SaveAsync(WorkflowDocument document, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = JsonSerializer.Serialize(document, JsonOptions);
            var response = await _client.SaveWorkflowAsync(
                new SaveWorkflowRequest
                {
                    WorkflowId = document.WorkflowId,
                    Name = document.Name,
                    JsonPayload = payload
                },
                cancellationToken: cancellationToken);

            return response.Success
                ? ApiResult<Unit>.Success(Unit.Value)
                : ApiResult<Unit>.ServerError(response.ErrorMessage);
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
