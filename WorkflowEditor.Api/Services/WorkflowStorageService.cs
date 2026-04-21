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
        
        var testDocument = """
                           {
                             "workflowId": "main-flow-id",
                             "name": "Основной процесс",
                             "steps": [
                               {
                                 "type": "subflow",
                                 "id": "node-1",
                                 "name": "Проверка оплаты",
                                 "position": { "x": 100, "y": 100 },
                                 "subflowId": "payment-logic-v1"
                               }
                             ],
                             "links": []
                           }
                           """;

        return Task.FromResult(new WorkflowDocumentResponse
        {
            WorkflowId = request.WorkflowId,
            Name = "Основной процесс",
            JsonPayload = testDocument
        });
    }
}