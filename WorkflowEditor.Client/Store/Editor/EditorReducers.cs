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
}