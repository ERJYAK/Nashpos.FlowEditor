using System.Text.Json;
using System.Text.RegularExpressions;
using Fluxor;
using WorkflowEditor.Client.Services.Api;
using WorkflowEditor.Client.Services.Topology;
using WorkflowEditor.Core.Models;
using WorkflowEditor.Core.Models.Steps;
using WorkflowEditor.Core.Serialization;

namespace WorkflowEditor.Client.Store.Editor;

public sealed class EditorEffects(IWorkflowApi api, IState<EditorState> state)
{
    private static readonly JsonSerializerOptions JsonOptions = JsonConfiguration.GetOptions();
    private static readonly Regex NameRegex = new(@"^[a-z0-9][a-z0-9-]*$", RegexOptions.Compiled);

    [EffectMethod]
    public Task HandleCreateWorkflowRequested(CreateWorkflowRequestedAction action, IDispatcher dispatcher)
    {
        // Если такой документ уже открыт — не дублируем, просто переключаемся.
        if (state.Value.OpenDocuments.ContainsKey(action.Name))
        {
            dispatcher.Dispatch(new SwitchTabAction(action.Name));
            return Task.CompletedTask;
        }

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

        // Гонка: пока шёл async-fetch, кэш мог быть заполнен через импорт файла
        // или открытие вкладки. Это не ошибка — отдаём готовые данные.
        if (state.Value.OpenDocuments.TryGetValue(action.Name, out var open))
        {
            dispatcher.Dispatch(new LoadSubflowSuccessAction(action.Name, open.Document));
            return;
        }
        if (state.Value.SubflowCache.TryGetValue(action.Name, out var cached))
        {
            dispatcher.Dispatch(new LoadSubflowSuccessAction(action.Name, cached));
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

        // Любая ошибка (NotFound / NetworkError / ServerError) — открываем пустой draft с этим именем.
        // Так пользователь всегда может «провалиться» в подпроцесс независимо от состояния сети.
        dispatcher.Dispatch(new CreateWorkflowRequestedAction(action.Name));
    }

    [EffectMethod]
    public Task HandleRenameWorkflowRequested(RenameWorkflowRequestedAction action, IDispatcher dispatcher)
    {
        var fail = ValidateRename(action.OldName, action.NewName);
        if (fail is not null)
        {
            dispatcher.Dispatch(new RenameWorkflowFailedAction(action.OldName, action.NewName, fail));
            return Task.CompletedTask;
        }

        if (action.OldName != action.NewName)
            dispatcher.Dispatch(new RenameWorkflowAction(action.OldName, action.NewName, action.CascadeSubflows));
        return Task.CompletedTask;
    }

    [EffectMethod]
    public Task HandleRenameSubflowRequested(RenameSubflowRequestedAction action, IDispatcher dispatcher)
    {
        if (string.IsNullOrWhiteSpace(action.NewSubflowName) || !NameRegex.IsMatch(action.NewSubflowName))
        {
            dispatcher.Dispatch(new RenameWorkflowFailedAction(string.Empty, action.NewSubflowName,
                "Имя подпроцесса должно быть из латиницы, цифр и дефиса"));
            return Task.CompletedTask;
        }

        if (!state.Value.OpenDocuments.TryGetValue(action.DocName, out var editor)) return Task.CompletedTask;
        var step = editor.Document.Steps.OfType<SubflowStep>().FirstOrDefault(s => s.Id == action.StepId);
        if (step is null) return Task.CompletedTask;
        var oldName = step.SubflowName;

        if (!action.Cascade || string.IsNullOrEmpty(oldName))
        {
            // «Нет» сценарий: только этот узел; автоматически подхватит другой workflow
            // с этим именем (если открыт), или будет «висеть» сам по себе.
            dispatcher.Dispatch(new UpdateSubflowNameAction(action.DocName, action.StepId, action.NewSubflowName));
            return Task.CompletedTask;
        }

        // «Да» сценарий: каскадно переименовать все ссылки + при наличии — вкладку.
        if (state.Value.OpenDocuments.ContainsKey(oldName))
            dispatcher.Dispatch(new RenameWorkflowRequestedAction(oldName, action.NewSubflowName, CascadeSubflows: true));
        else
            dispatcher.Dispatch(new CascadeRenameSubflowReferencesAction(oldName, action.NewSubflowName));
        return Task.CompletedTask;
    }

    private string? ValidateRename(string oldName, string newName)
    {
        if (string.IsNullOrWhiteSpace(newName)) return "Имя обязательно";
        if (newName.Length > 64) return "Не больше 64 символов";
        if (!NameRegex.IsMatch(newName)) return "Допустимы только латиница, цифры и дефис";
        if (newName == oldName) return null;
        if (state.Value.OpenDocuments.ContainsKey(newName))
            return $"Холст с именем «{newName}» уже открыт";
        return null;
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
