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
}