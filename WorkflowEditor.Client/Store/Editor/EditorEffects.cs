using Fluxor;
using WorkflowEditor.Client.Services.Api;
using WorkflowEditor.Core.Models;

namespace WorkflowEditor.Client.Store.Editor;

public class EditorEffects
{
    private readonly IWorkflowApi _api;
    private readonly IState<EditorState> _state;

    public EditorEffects(IWorkflowApi api, IState<EditorState> state)
    {
        _api = api;
        _state = state;
    }

    [EffectMethod]
    public async Task HandleLoadWorkflow(LoadWorkflowAction action, IDispatcher dispatcher)
    {
        var result = await _api.GetAsync(action.WorkflowId);
        if (result.IsSuccess && result.Value is not null)
        {
            dispatcher.Dispatch(new LoadWorkflowSuccessAction(result.Value));
            return;
        }

        var message = result.Outcome switch
        {
            ApiOutcome.NotFound => $"Процесс «{action.WorkflowId}» не найден",
            _ => result.ErrorMessage ?? $"Не удалось загрузить процесс «{action.WorkflowId}»"
        };
        dispatcher.Dispatch(new LoadWorkflowFailedAction(message));
    }

    [EffectMethod]
    public async Task HandleSaveWorkflow(SaveWorkflowAction action, IDispatcher dispatcher)
    {
        if (!_state.Value.OpenDocuments.TryGetValue(action.WorkflowId, out var document))
        {
            dispatcher.Dispatch(new SaveWorkflowFailedAction(action.WorkflowId, "Документ не найден в памяти"));
            return;
        }

        var result = await _api.SaveAsync(document);
        if (result.IsSuccess)
        {
            dispatcher.Dispatch(new SaveWorkflowSuccessAction(action.WorkflowId));
            return;
        }

        var message = result.ErrorMessage ?? result.Outcome.ToString();
        dispatcher.Dispatch(new SaveWorkflowFailedAction(action.WorkflowId, message));
    }

    [EffectMethod]
    public Task HandleCreateWorkflowRequested(CreateWorkflowRequestedAction action, IDispatcher dispatcher)
    {
        var document = new WorkflowDocument
        {
            WorkflowId = Guid.NewGuid().ToString(),
            Name = $"Процесс {_state.Value.OpenDocuments.Count + 1}"
        };
        dispatcher.Dispatch(new OpenWorkflowAction(document));
        return Task.CompletedTask;
    }
}
