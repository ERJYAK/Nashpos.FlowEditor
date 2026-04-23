using Blazor.Diagrams.Core.Anchors;
using Blazor.Diagrams.Core.Models;
using WorkflowEditor.Client.Diagram.Nodes;
using WorkflowEditor.Core.Models.Steps;

namespace WorkflowEditor.Client.Store.Editor;

using System.Collections.Immutable;
using Fluxor;
using WorkflowEditor.Core.Models;

public static class EditorReducers
{
    [ReducerMethod]
    public static EditorState ReduceDeleteSelectedItemAction(EditorState state, DeleteSelectedItemAction action)
    {
        if (state.ActiveDocumentId == null || !state.OpenDocuments.TryGetValue(state.ActiveDocumentId, out var document))
        {
            return state;
        }

        var updatedDocument = document;

        switch (action.SelectedItem)
        {
            case WorkflowNodeModel nodeModel:
            {
                var newSteps = document.Steps.Where(s => s.Id != nodeModel.StepId).ToList();
                var newLinks = document.Links
                    .Where(l => l.SourceNodeId != nodeModel.StepId && l.TargetNodeId != nodeModel.StepId)
                    .ToList();
                updatedDocument = document with { Steps = newSteps, Links = newLinks };
                break;
            }
            case LinkModel linkModel:
            {
                var sourcePort = (linkModel.Source as SinglePortAnchor)?.Port;
                var targetPort = (linkModel.Target as SinglePortAnchor)?.Port;

                if (sourcePort?.Parent is WorkflowNodeModel sourceNode && targetPort?.Parent is WorkflowNodeModel targetNode)
                {
                    var linkToRemove = document.Links.FirstOrDefault(l =>
                        l.SourceNodeId == sourceNode.StepId &&
                        l.TargetNodeId == targetNode.StepId &&
                        l.SourcePortId == sourcePort.Alignment.ToString() &&
                        l.TargetPortId == targetPort.Alignment.ToString());

                    if (linkToRemove != null)
                    {
                        var newLinks = document.Links.Where(l => l.Id != linkToRemove.Id).ToList();
                        updatedDocument = document with { Links = newLinks };
                    }
                }
                break;
            }
        }

        if (updatedDocument != document)
        {
            return state with { OpenDocuments = state.OpenDocuments.SetItem(state.ActiveDocumentId, updatedDocument) };
        }

        return state;
    }

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
    
    // Добавь этот метод в EditorReducers.cs
    [ReducerMethod]
    public static EditorState ReduceRenameStepAction(EditorState state, RenameStepAction action)
    {
        if (!state.OpenDocuments.TryGetValue(action.WorkflowId, out var document)) return state;

        var newSteps = document.Steps.Select(s => 
        {
            if (s.Id != action.StepId) return s;
        
            // Полиморфное обновление в зависимости от типа шага
            if (s is SubflowStep sub) return sub with { Name = action.NewName };
            if (s is BaseStep b) return b with { Name = action.NewName };
            return s;
        }).ToList();

        var updatedDocument = document with { Links = document.Links, Steps = newSteps };
        return state with { OpenDocuments = state.OpenDocuments.SetItem(action.WorkflowId, updatedDocument) };
    }

    [ReducerMethod]
    public static EditorState ReduceStartEditingStepAction(EditorState state, StartEditingStepAction action)
    {
        return state with { EditingStepId = action.StepId };
    }

    [ReducerMethod]
    public static EditorState ReduceStopEditingStepAction(EditorState state, StopEditingStepAction action)
    {
        return state with { EditingStepId = null };
    }
}