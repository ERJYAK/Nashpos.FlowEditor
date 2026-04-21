using WorkflowEditor.Core.Models.Steps;

namespace WorkflowEditor.Client.Store.Editor;

using System.Collections.Immutable;
using Fluxor;
using WorkflowEditor.Core.Models;

public static class EditorReducers
{
    [ReducerMethod]
    public static EditorState ReduceOpenWorkflowAction(EditorState state, OpenWorkflowAction action)
    {
        // Добавляем или обновляем документ в словаре
        var newDocuments = state.OpenDocuments.SetItem(action.Document.WorkflowId, action.Document);
        
        return state with
        {
            OpenDocuments = newDocuments,
            ActiveDocumentId = action.Document.WorkflowId
        };
    }

    [ReducerMethod]
    public static EditorState ReduceAddStepAction(EditorState state, AddStepAction action)
    {
        if (!state.OpenDocuments.TryGetValue(action.WorkflowId, out var document))
            return state;

        // Создаем новый список шагов и новый документ, чтобы сохранить иммутабельность
        var newSteps = document.Steps.ToList();
        newSteps.Add(action.Step);
        
        var updatedDocument = document with { Steps = newSteps };
        
        return state with
        {
            OpenDocuments = state.OpenDocuments.SetItem(action.WorkflowId, updatedDocument)
        };
    }
    
    [ReducerMethod]
    public static EditorState ReduceLoadWorkflowAction(EditorState state, LoadWorkflowAction action)
    {
        // Включаем индикатор загрузки (опционально для UI)
        return state with { IsLoading = true };
    }

    [ReducerMethod]
    public static EditorState ReduceLoadWorkflowSuccessAction(EditorState state, LoadWorkflowSuccessAction action)
    {
        // Добавляем новый документ в словарь OpenDocuments
        // Если документ с таким ID уже открыт, он будет обновлен (Source of Truth)
        var newDocuments = state.OpenDocuments.SetItem(action.Document.WorkflowId, action.Document);

        return state with
        {
            IsLoading = false,
            OpenDocuments = newDocuments,
            ActiveDocumentId = action.Document.WorkflowId // Переключаем фокус на новую вкладку
        };
    }

    [ReducerMethod]
    public static EditorState ReduceLoadWorkflowFailedAction(EditorState state, LoadWorkflowFailedAction action)
    {
        // Выключаем лоадер при ошибке
        return state with { IsLoading = false };
    }
    
    [ReducerMethod]
    public static EditorState ReduceCreateNewWorkflowAction(EditorState state, CreateNewWorkflowAction action)
    {
        Console.WriteLine("[Слой Редуктора] Пойман экшен CreateNewWorkflowAction");
        try
        {
            var newDoc = new WorkflowDocument
            {
                WorkflowId = Guid.NewGuid().ToString(),
                Name = $"Процесс {state.OpenDocuments.Count + 1}",
                Steps = new List<WorkflowStep>(),
                Links = new List<WorkflowLink>()
            };

            // Используем SetItem вместо Add на случай, если документ с таким ID чудом уже есть
            var newDocuments = state.OpenDocuments.SetItem(newDoc.WorkflowId, newDoc);
            
            Console.WriteLine($"[Слой Редуктора] Документ создан. Теперь в памяти документов: {newDocuments.Count}");

            return state with
            {
                OpenDocuments = newDocuments,
                ActiveDocumentId = newDoc.WorkflowId
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Слой Редуктора ОШИБКА] {ex.Message}");
            return state; // Возвращаем старый стейт при ошибке
        }
    }
    
    [ReducerMethod]
    public static EditorState ReduceAddLinkAction(EditorState state, AddLinkAction action)
    {
        if (!state.OpenDocuments.TryGetValue(action.WorkflowId, out var document)) return state;

        var newLinks = document.Links.ToList();
        // Защита от дубликатов (хотя Fluxor сам по себе идемпотентен, это полезно)
        if (!newLinks.Any(l => l.Id == action.Link.Id))
        {
            newLinks.Add(action.Link);
        }

        var updatedDocument = document with { Links = newLinks };
        return state with { OpenDocuments = state.OpenDocuments.SetItem(action.WorkflowId, updatedDocument) };
    }

    [ReducerMethod]
    public static EditorState ReduceRemoveLinkAction(EditorState state, RemoveLinkAction action)
    {
        if (!state.OpenDocuments.TryGetValue(action.WorkflowId, out var document)) return state;

        var newLinks = document.Links.Where(l => l.Id != action.LinkId).ToList();
        var updatedDocument = document with { Links = newLinks };
        
        return state with { OpenDocuments = state.OpenDocuments.SetItem(action.WorkflowId, updatedDocument) };
    }
    
    [ReducerMethod]
    public static EditorState ReduceSwitchTabAction(EditorState state, SwitchTabAction action)
    {
        return state with { ActiveDocumentId = action.WorkflowId };
    }
    
    [ReducerMethod]
    public static EditorState ReduceMoveStepAction(EditorState state, MoveStepAction action)
    {
        if (!state.OpenDocuments.TryGetValue(action.WorkflowId, out var document)) return state;

        // Клонируем список шагов и обновляем координаты нужного узла
        var newSteps = document.Steps.Select(s => 
        {
            if (s.Id != action.StepId) return s;
            
            // Используем 'with' для сохранения иммутабельности конкретного типа
            if (s is SubflowStep sub) return sub with { Position = action.NewPosition };
            if (s is BaseStep b) return b with { Position = action.NewPosition };
            return s;
        }).ToList();

        var updatedDocument = document with { Steps = newSteps };
        return state with { OpenDocuments = state.OpenDocuments.SetItem(action.WorkflowId, updatedDocument) };
    }
    
}