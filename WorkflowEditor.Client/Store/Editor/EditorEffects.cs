namespace WorkflowEditor.Client.Store.Editor;

using System.Text.Json;
using Fluxor;
using WorkflowEditor.Contracts.Grpc; // Пространство имен сгенерированного gRPC клиента
using WorkflowEditor.Core.Serialization;

public class EditorEffects
{
    private readonly WorkflowStorage.WorkflowStorageClient _grpcClient;
    private readonly IState<EditorState> _editorState;

    // Внедряем gRPC клиент и доступ к текущему состоянию
    public EditorEffects(WorkflowStorage.WorkflowStorageClient grpcClient, IState<EditorState> editorState)
    {
        _grpcClient = grpcClient;
        _editorState = editorState;
    }

    [EffectMethod]
    public async Task HandleSaveWorkflow(SaveWorkflowAction action, IDispatcher dispatcher)
    {
        // 1. Ищем нужный документ в локальном стейте
        if (!_editorState.Value.OpenDocuments.TryGetValue(action.WorkflowId, out var document))
        {
            dispatcher.Dispatch(new SaveWorkflowFailedAction(action.WorkflowId, "Документ не найден в памяти"));
            return;
        }

        try
        {
            // 2. Сериализуем документ, используя нашу единую конфигурацию из Core
            // Бэкенд получит чистую строку, не зная о типах узлов
            var jsonPayload = JsonSerializer.Serialize(document, JsonConfiguration.GetOptions());

            // 3. Формируем gRPC запрос
            var request = new SaveWorkflowRequest
            {
                WorkflowId = document.WorkflowId,
                Name = document.Name,
                JsonPayload = jsonPayload
            };

            // 4. Отправляем на сервер
            var response = await _grpcClient.SaveWorkflowAsync(request);

            // 5. Диспетчеризируем результат
            if (response.Success)
            {
                dispatcher.Dispatch(new SaveWorkflowSuccessAction(action.WorkflowId));
                
                // Здесь же можно вызвать какой-нибудь SnackbarAction, 
                // чтобы показать пользователю зеленую плашку "Сохранено"
            }
            else
            {
                dispatcher.Dispatch(new SaveWorkflowFailedAction(action.WorkflowId, response.ErrorMessage));
            }
        }
        catch (Exception ex)
        {
            // Ловим сетевые ошибки (например, сервер недоступен)
            dispatcher.Dispatch(new SaveWorkflowFailedAction(action.WorkflowId, ex.Message));
        }
    }
}