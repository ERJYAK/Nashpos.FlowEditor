using Blazor.Diagrams.Core.Anchors;
using Blazor.Diagrams.Core.Models;
using Fluxor;
using WorkflowEditor.Client.Diagram.Nodes;
using WorkflowEditor.Core.Models;

namespace WorkflowEditor.Client.Store.Editor;

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

        foreach (var link in document.Links)
        {
            if (selectedStepIds.Contains(link.SourceNodeId) &&
                selectedStepIds.Contains(link.TargetNodeId))
            {
                copiedLinks.Add(link);
                copiedLinkIds.Add(link.Id);
            }
        }

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
        if (clipboard.Steps.Count == 0 && clipboard.Links.Count == 0) return state;

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
        return state.WithMutation(state.ActiveDocumentId, updatedDocument);
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
        return state.WithMutation(state.ActiveDocumentId, updatedDocument);
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
        return state with
        {
            OpenDocuments = state.OpenDocuments.SetItem(action.Document.WorkflowId, action.Document),
            ActiveDocumentId = action.Document.WorkflowId,
            DirtyDocuments = state.DirtyDocuments.Remove(action.Document.WorkflowId)
        };
    }

    [ReducerMethod]
    public static EditorState ReduceCloseTabAction(EditorState state, CloseTabAction action)
    {
        if (!state.OpenDocuments.ContainsKey(action.WorkflowId)) return state;

        var remaining = state.OpenDocuments.Remove(action.WorkflowId);
        var nextActive = state.ActiveDocumentId == action.WorkflowId
            ? remaining.Keys.FirstOrDefault()
            : state.ActiveDocumentId;

        return state with
        {
            OpenDocuments = remaining,
            ActiveDocumentId = nextActive,
            DirtyDocuments = state.DirtyDocuments.Remove(action.WorkflowId)
        };
    }

    [ReducerMethod]
    public static EditorState ReduceAddStepAction(EditorState state, AddStepAction action)
    {
        if (!state.OpenDocuments.TryGetValue(action.WorkflowId, out var document))
            return state;

        var newSteps = document.Steps.ToList();
        newSteps.Add(action.Step);

        var updatedDocument = document with { Steps = newSteps };
        return state.WithMutation(action.WorkflowId, updatedDocument);
    }

    [ReducerMethod]
    public static EditorState ReduceLoadWorkflowAction(EditorState state, LoadWorkflowAction action)
    {
        return state with { IsLoading = true };
    }

    [ReducerMethod]
    public static EditorState ReduceLoadWorkflowSuccessAction(EditorState state, LoadWorkflowSuccessAction action)
    {
        return state with
        {
            IsLoading = false,
            OpenDocuments = state.OpenDocuments.SetItem(action.Document.WorkflowId, action.Document),
            ActiveDocumentId = action.Document.WorkflowId,
            DirtyDocuments = state.DirtyDocuments.Remove(action.Document.WorkflowId)
        };
    }

    [ReducerMethod]
    public static EditorState ReduceLoadWorkflowFailedAction(EditorState state, LoadWorkflowFailedAction action)
    {
        return state with { IsLoading = false };
    }

    [ReducerMethod]
    public static EditorState ReduceSaveWorkflowSuccessAction(EditorState state, SaveWorkflowSuccessAction action)
    {
        return state with { DirtyDocuments = state.DirtyDocuments.Remove(action.WorkflowId) };
    }

    [ReducerMethod]
    public static EditorState ReduceAddLinkAction(EditorState state, AddLinkAction action)
    {
        if (!state.OpenDocuments.TryGetValue(action.WorkflowId, out var document)) return state;
        if (document.Links.Any(l => l.Id == action.Link.Id)) return state;

        var newLinks = document.Links.ToList();
        newLinks.Add(action.Link);

        var updatedDocument = document with { Links = newLinks };
        return state.WithMutation(action.WorkflowId, updatedDocument);
    }

    [ReducerMethod]
    public static EditorState ReduceRemoveLinkAction(EditorState state, RemoveLinkAction action)
    {
        if (!state.OpenDocuments.TryGetValue(action.WorkflowId, out var document)) return state;
        if (document.Links.All(l => l.Id != action.LinkId)) return state;

        var newLinks = document.Links.Where(l => l.Id != action.LinkId).ToList();
        var updatedDocument = document with { Links = newLinks };
        return state.WithMutation(action.WorkflowId, updatedDocument);
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
        return state.WithMutation(action.WorkflowId, updatedDocument);
    }

    [ReducerMethod]
    public static EditorState ReduceRemoveStepAction(EditorState state, RemoveStepAction action)
    {
        if (!state.OpenDocuments.TryGetValue(action.WorkflowId, out var document))
            return state;
        if (document.Steps.All(s => s.Id != action.StepId)) return state;

        var newSteps = document.Steps.Where(s => s.Id != action.StepId).ToList();

        var newLinks = document.Links.Where(l =>
            l.SourceNodeId != action.StepId && l.TargetNodeId != action.StepId).ToList();

        var updatedDocument = document with { Steps = newSteps, Links = newLinks };
        return state.WithMutation(action.WorkflowId, updatedDocument);
    }

    [ReducerMethod]
    public static EditorState ReduceRenameStepAction(EditorState state, RenameStepAction action)
    {
        if (!state.OpenDocuments.TryGetValue(action.WorkflowId, out var document)) return state;

        var changed = false;
        var newSteps = document.Steps
            .Select(s =>
            {
                if (s.Id != action.StepId || s.Name == action.NewName) return s;
                changed = true;
                return s.WithName(action.NewName);
            })
            .ToList();

        if (!changed) return state;

        var updatedDocument = document with { Steps = newSteps };
        return state.WithMutation(action.WorkflowId, updatedDocument);
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

    private static EditorState WithMutation(this EditorState state, string workflowId, WorkflowDocument updated) =>
        state with
        {
            OpenDocuments = state.OpenDocuments.SetItem(workflowId, updated),
            DirtyDocuments = state.DirtyDocuments.Add(workflowId)
        };
}
