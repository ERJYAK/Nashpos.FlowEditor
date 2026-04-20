namespace WorkflowEditor.Api.Services;

using Grpc.Core;
using WorkflowEditor.Contracts.Grpc;

public class WorkflowStorageService : WorkflowStorage.WorkflowStorageBase
{
    // Пока реализуем заглушку. В будущем здесь будет DI (например, IRepository) 
    // для сохранения jsonPayload в базу данных или файловую систему.

    public override Task<SaveWorkflowResponse> SaveWorkflow(SaveWorkflowRequest request, ServerCallContext context)
    {
        Console.WriteLine($"[gRPC] Сохранение документа: {request.WorkflowId}");
        Console.WriteLine($"[gRPC] Payload size: {request.JsonPayload.Length} chars");

        return Task.FromResult(new SaveWorkflowResponse
        {
            Success = true,
            ErrorMessage = string.Empty
        });
    }

    public override Task<WorkflowDocumentResponse> GetWorkflow(GetWorkflowRequest request, ServerCallContext context)
    {
        Console.WriteLine($"[gRPC] Загрузка документа: {request.WorkflowId}");

        return Task.FromResult(new WorkflowDocumentResponse
        {
            WorkflowId = request.WorkflowId,
            Name = "New Workflow",
            // Для теста возвращаем пустой граф с валидной JSON-структурой
            JsonPayload = """{"workflowId":"123","name":"Test","steps":[],"links":[]}""" 
        });
    }
}