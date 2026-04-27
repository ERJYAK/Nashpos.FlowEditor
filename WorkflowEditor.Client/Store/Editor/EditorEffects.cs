using System.Text.Json;
using Fluxor;
using WorkflowEditor.Client.Services.Api;
using WorkflowEditor.Client.Services.Topology;
using WorkflowEditor.Core.Models;
using WorkflowEditor.Core.Serialization;

namespace WorkflowEditor.Client.Store.Editor;

public sealed class EditorEffects(IWorkflowApi api, IState<EditorState> state)
{
    private static readonly JsonSerializerOptions JsonOptions = JsonConfiguration.GetOptions();

    [EffectMethod]
    public Task HandleCreateWorkflowRequested(CreateWorkflowRequestedAction action, IDispatcher dispatcher)
    {
        var document = new WorkflowDocument
        {
            Name = action.Name,
            Description = string.Empty
        };
        dispatcher.Dispatch(new OpenWorkflowAction(document));
        return Task.CompletedTask;
    }

    [EffectMethod]
    public async Task HandleLoadWorkflow(LoadWorkflowAction action, IDispatcher dispatcher)
    {
        var result = await api.GetAsync(action.Name);
        if (result.IsSuccess && result.Value is not null)
        {
            dispatcher.Dispatch(new LoadWorkflowSuccessAction(result.Value));
            return;
        }

        var message = result.Outcome switch
        {
            ApiOutcome.NotFound => $"Процесс «{action.Name}» не найден",
            _ => result.ErrorMessage ?? $"Не удалось загрузить процесс «{action.Name}»"
        };
        dispatcher.Dispatch(new LoadWorkflowFailedAction(action.Name, message));
    }

    [EffectMethod]
    public async Task HandleSaveWorkflow(SaveWorkflowAction action, IDispatcher dispatcher)
    {
        if (!state.Value.OpenDocuments.TryGetValue(action.Name, out var editor))
        {
            dispatcher.Dispatch(new SaveWorkflowFailedAction(action.Name, "Документ не найден в памяти"));
            return;
        }

        var ordering = StepOrderResolver.Resolve(editor.Document.Steps, editor.Links);
        if (!ordering.IsSuccess)
        {
            dispatcher.Dispatch(new SaveWorkflowFailedAction(action.Name, ordering.ErrorMessage!));
            return;
        }

        var documentToSave = editor.Document with { Steps = ordering.Ordered! };
        var result = await api.SaveAsync(documentToSave);
        if (result.IsSuccess)
        {
            dispatcher.Dispatch(new SaveWorkflowSuccessAction(action.Name));
            return;
        }

        var message = result.ErrorMessage ?? result.Outcome.ToString();
        dispatcher.Dispatch(new SaveWorkflowFailedAction(action.Name, message));
    }

    [EffectMethod]
    public async Task HandleLoadSubflow(LoadSubflowAction action, IDispatcher dispatcher)
    {
        var result = await api.GetAsync(action.Name);
        if (result.IsSuccess && result.Value is not null)
        {
            dispatcher.Dispatch(new LoadSubflowSuccessAction(action.Name, result.Value));
            return;
        }

        var message = result.Outcome switch
        {
            ApiOutcome.NotFound => $"Подпроцесс «{action.Name}» не существует",
            _ => result.ErrorMessage ?? $"Не удалось загрузить подпроцесс «{action.Name}»"
        };
        dispatcher.Dispatch(new LoadSubflowFailedAction(action.Name, message));
    }

    [EffectMethod]
    public async Task HandleOpenSubflow(OpenSubflowAction action, IDispatcher dispatcher)
    {
        if (state.Value.OpenDocuments.ContainsKey(action.Name))
        {
            dispatcher.Dispatch(new SwitchTabAction(action.Name));
            return;
        }

        var result = await api.GetAsync(action.Name);
        if (result.IsSuccess && result.Value is not null)
        {
            dispatcher.Dispatch(new OpenWorkflowAction(result.Value));
            return;
        }

        if (result.Outcome == ApiOutcome.NotFound)
        {
            dispatcher.Dispatch(new CreateWorkflowRequestedAction(action.Name));
            return;
        }

        var message = result.ErrorMessage ?? $"Не удалось открыть подпроцесс «{action.Name}»";
        dispatcher.Dispatch(new LoadWorkflowFailedAction(action.Name, message));
    }

    [EffectMethod]
    public Task HandleImportFileRequested(ImportFileRequestedAction action, IDispatcher dispatcher)
    {
        var name = Path.GetFileNameWithoutExtension(action.FileName);
        if (string.IsNullOrWhiteSpace(name))
        {
            dispatcher.Dispatch(new ImportFileFailedAction(action.FileName, "Имя файла пустое"));
            return Task.CompletedTask;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<WorkflowDocument>(action.Payload, JsonOptions);
            if (parsed is null)
            {
                dispatcher.Dispatch(new ImportFileFailedAction(action.FileName, "Файл пустой"));
                return Task.CompletedTask;
            }

            dispatcher.Dispatch(new OpenWorkflowAction(parsed with { Name = name }));
        }
        catch (JsonException ex)
        {
            dispatcher.Dispatch(new ImportFileFailedAction(action.FileName, $"Не удалось разобрать JSON: {ex.Message}"));
        }

        return Task.CompletedTask;
    }
}
