using Blazor.Diagrams.Core.Anchors;
using Blazor.Diagrams.Core.Models;
using WorkflowEditor.Client.Diagram.Nodes;

namespace WorkflowEditor.Client.Store.Editor;

using Fluxor;
using WorkflowEditor.Core.Models;

public static class EditorReducers
{
    [ReducerMethod]
    public static EditorState ReduceCopySelectionAction(EditorState state, CopySelectionAction action)
    {
        if (state.ActiveDocumentId == null ||
            !state.OpenDocuments.TryGetValue(state.ActiveDocumentId, out var document))
        {
            return state;
        }

        var selectedStepIds = action.Selected
            .OfType<WorkflowNodeModel>()
            .Select(n => n.StepId)
            .ToHashSet();

        var copiedSteps = document.Steps
            .Where(s => selectedStepIds.Contains(s.Id))
            .ToList();

        if (copiedSteps.Count == 0 && !action.Selected.OfType<LinkModel>().Any())
        {
            return state;
        }

        var copiedLinkIds = new HashSet<string>();
        var copiedLinks = new List<WorkflowLink>();

        // Линки между выделенными узлами
        foreach (var link in document.Links)
        {
            if (selectedStepIds.Contains(link.SourceNodeId) &&
                selectedStepIds.Contains(link.TargetNodeId))
            {
                copiedLinks.Add(link);
                copiedLinkIds.Add(link.Id);
            }
        }

        // Явно выделенные линки (даже если один из их узлов не выделен — копируем, если оба узла существуют)
        foreach (var linkModel in action.Selected.OfType<LinkModel>())
        {
            var stateLink = ResolveStateLink(linkModel, document);
            if (stateLink != null && copiedLinkIds.Add(stateLink.Id))
            {
                copiedLinks.Add(stateLink);
            }
        }

        var origin = copiedSteps.Count > 0
            ? new CanvasPosition(
                copiedSteps.Min(s => s.Position.X),
                copiedSteps.Min(s => s.Position.Y))
            : new CanvasPosition(0, 0);

        return state with
        {
            Clipboard = new ClipboardPayload(copiedSteps, copiedLinks, origin)
        };
    }

    [ReducerMethod]
    public static EditorState ReducePasteClipboardAction(EditorState state, PasteClipboardAction action)
    {
        if (state.Clipboard == null ||
            state.ActiveDocumentId == null ||
            !state.OpenDocuments.TryGetValue(state.ActiveDocumentId, out var document))
        {
            return state;
        }

        var clipboard = state.Clipboard;
        var idMap = new Dictionary<string, string>();
        var newSteps = document.Steps.ToList();

        foreach (var step in clipboard.Steps)
        {
            var newId = Guid.NewGuid().ToString();
            idMap[step.Id] = newId;

            var newPosition = new CanvasPosition(
                step.Position.X - clipboard.Origin.X + action.X,
                step.Position.Y - clipboard.Origin.Y + action.Y);

            var cloned = step
                .CloneWithId(newId)
                .WithName(step.Name + " (Copy)")
                .WithPosition(newPosition);

            newSteps.Add(cloned);
        }

        var newLinks = document.Links.ToList();
        foreach (var link in clipboard.Links)
        {
            if (!idMap.TryGetValue(link.SourceNodeId, out var newSourceId) ||
                !idMap.TryGetValue(link.TargetNodeId, out var newTargetId))
            {
                // Один из концов линка не входит в буфер — пропускаем, чтобы не тянуть «висящие» связи
                continue;
            }

            newLinks.Add(new WorkflowLink
            {
                Id = Guid.NewGuid().ToString(),
                SourceNodeId = newSourceId,
                SourcePortId = link.SourcePortId,
                TargetNodeId = newTargetId,
                TargetPortId = link.TargetPortId,
                Label = link.Label
            });
        }
        
        var updatedDocument = document with { Steps = newSteps, Links = newLinks };
        return state with
        {
            OpenDocuments = state.OpenDocuments.SetItem(state.ActiveDocumentId, updatedDocument)
        };
    }

    [ReducerMethod]
    public static EditorState ReduceDeleteSelectionAction(EditorState state, DeleteSelectionAction action)
    {
        if (state.ActiveDocumentId == null ||
            !state.OpenDocuments.TryGetValue(state.ActiveDocumentId, out var document))
        {
            return state;
        }

        var nodeIds = action.Selected
            .OfType<WorkflowNodeModel>()
            .Select(n => n.StepId)
            .ToHashSet();

        var explicitLinkIds = new HashSet<string>();

        foreach (var linkModel in action.Selected.OfType<LinkModel>())
        {
            var stateLink = ResolveStateLink(linkModel, document);
            if (stateLink != null)
                explicitLinkIds.Add(stateLink.Id);
        }
        
        if (nodeIds.Count == 0 && explicitLinkIds.Count == 0)
        {
            return state;
        }
        
        var newSteps = document.Steps
            .Where(s => !nodeIds.Contains(s.Id))
            .ToList();

        var newLinks = document.Links
            .Where(l =>
                !nodeIds.Contains(l.SourceNodeId) &&
                !nodeIds.Contains(l.TargetNodeId) &&
                !explicitLinkIds.Contains(l.Id))
            .ToList();

        var updatedDocument = document with { Steps = newSteps, Links = newLinks };
        return state with
        {
            OpenDocuments = state.OpenDocuments.SetItem(state.ActiveDocumentId, updatedDocument)
        };
    }

    private static WorkflowLink? ResolveStateLink(LinkModel linkModel, WorkflowDocument document)
    {
        var sourcePort = (linkModel.Source as SinglePortAnchor)?.Port;
        var targetPort = (linkModel.Target as SinglePortAnchor)?.Port;

        if (sourcePort?.Parent is WorkflowNodeModel sourceNode &&
            targetPort?.Parent is WorkflowNodeModel targetNode)
        {
            return document.Links.FirstOrDefault(l =>
                l.SourceNodeId == sourceNode.StepId &&
                l.TargetNodeId == targetNode.StepId &&
                l.SourcePortId == sourcePort.Alignment.ToString() &&
                l.TargetPortId == targetPort.Alignment.ToString());
        }
        return null;
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
    public static EditorState ReduceMoveStepsAction(EditorState state, MoveStepsAction action)
    {
        if (action.Moves.Count == 0) return state;
        if (!state.OpenDocuments.TryGetValue(action.WorkflowId, out var document)) return state;
        var moves = action.Moves.ToDictionary(m => m.StepId, m => m.NewPosition);

        var changed = false;
        var newSteps = document.Steps.Select(s =>
        {
            if (!moves.TryGetValue(s.Id, out var newPosition)) return s;
            if (s.Position == newPosition) return s;

            changed = true;
            return s.WithPosition(newPosition);
        }).ToList();
        
        if (!changed) return state;

        var updatedDocument = document with { Steps = newSteps };
        return state with { OpenDocuments = state.OpenDocuments.SetItem(action.WorkflowId, updatedDocument) };
    }
    
    [ReducerMethod]
    public static EditorState ReduceRemoveStepAction(EditorState state, RemoveStepAction action)
    {
        if (!state.OpenDocuments.TryGetValue(action.WorkflowId, out var document)) 
            return state;

        // Удаляем сам шаг
        var newSteps = document.Steps.Where(s => s.Id != action.StepId).ToList();
    
        // Зачищаем все связанные линки (страховка на уровне источника истины)
        var newLinks = document.Links.Where(l => 
            l.SourceNodeId != action.StepId && l.TargetNodeId != action.StepId).ToList();

        var updatedDocument = document with { Steps = newSteps, Links = newLinks };
    
        return state with 
        { 
            OpenDocuments = state.OpenDocuments.SetItem(action.WorkflowId, updatedDocument) 
        };
    }
    
    [ReducerMethod]
    public static EditorState ReduceRenameStepAction(EditorState state, RenameStepAction action)
    {
        if (!state.OpenDocuments.TryGetValue(action.WorkflowId, out var document)) return state;

        var newSteps = document.Steps
            .Select(s => s.Id == action.StepId ? s.WithName(action.NewName) : s)
            .ToList();

        var updatedDocument = document with { Steps = newSteps };
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