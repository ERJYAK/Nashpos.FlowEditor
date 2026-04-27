using System.Collections.Immutable;
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

        var copiedSteps = document.Steps.Values
            .Where(s => selectedStepIds.Contains(s.Id))
            .ToList();

        if (copiedSteps.Count == 0 && !action.Selected.OfType<LinkModel>().Any())
        {
            return state;
        }

        var copiedLinkIds = new HashSet<string>();
        var copiedLinks = new List<WorkflowLink>();

        foreach (var link in document.Links.Values)
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
        var newSteps = document.Steps.ToBuilder();

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

            newSteps[newId] = cloned;
        }

        var newLinks = document.Links.ToBuilder();
        foreach (var link in clipboard.Links)
        {
            if (!idMap.TryGetValue(link.SourceNodeId, out var newSourceId) ||
                !idMap.TryGetValue(link.TargetNodeId, out var newTargetId))
            {
                continue;
            }

            var newLinkId = Guid.NewGuid().ToString();
            newLinks[newLinkId] = new WorkflowLink
            {
                Id = newLinkId,
                SourceNodeId = newSourceId,
                SourcePortId = link.SourcePortId,
                TargetNodeId = newTargetId,
                TargetPortId = link.TargetPortId,
                Label = link.Label
            };
        }

        var updatedDocument = document with
        {
            Steps = newSteps.ToImmutable(),
            Links = newLinks.ToImmutable()
        };
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

        var newSteps = document.Steps.RemoveRange(nodeIds);

        var newLinks = document.Links.Values
            .Where(l =>
                !nodeIds.Contains(l.SourceNodeId) &&
                !nodeIds.Contains(l.TargetNodeId) &&
                !explicitLinkIds.Contains(l.Id))
            .ToImmutableDictionary(l => l.Id);

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
            return document.Links.Values.FirstOrDefault(l =>
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
            DirtyDocuments = state.DirtyDocuments.Remove(action.Document.WorkflowId),
            UndoStacks = state.UndoStacks.Remove(action.Document.WorkflowId),
            RedoStacks = state.RedoStacks.Remove(action.Document.WorkflowId)
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
            DirtyDocuments = state.DirtyDocuments.Remove(action.WorkflowId),
            UndoStacks = state.UndoStacks.Remove(action.WorkflowId),
            RedoStacks = state.RedoStacks.Remove(action.WorkflowId)
        };
    }

    [ReducerMethod]
    public static EditorState ReduceAddStepAction(EditorState state, AddStepAction action)
    {
        if (!state.OpenDocuments.TryGetValue(action.WorkflowId, out var document))
            return state;
        if (document.Steps.ContainsKey(action.Step.Id)) return state;

        var updatedDocument = document with { Steps = document.Steps.SetItem(action.Step.Id, action.Step) };
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
            DirtyDocuments = state.DirtyDocuments.Remove(action.Document.WorkflowId),
            UndoStacks = state.UndoStacks.Remove(action.Document.WorkflowId),
            RedoStacks = state.RedoStacks.Remove(action.Document.WorkflowId)
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
        if (document.Links.ContainsKey(action.Link.Id)) return state;

        var updatedDocument = document with { Links = document.Links.SetItem(action.Link.Id, action.Link) };
        return state.WithMutation(action.WorkflowId, updatedDocument);
    }

    [ReducerMethod]
    public static EditorState ReduceRemoveLinkAction(EditorState state, RemoveLinkAction action)
    {
        if (!state.OpenDocuments.TryGetValue(action.WorkflowId, out var document)) return state;
        if (!document.Links.ContainsKey(action.LinkId)) return state;

        var updatedDocument = document with { Links = document.Links.Remove(action.LinkId) };
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

        var stepsBuilder = document.Steps.ToBuilder();
        var changed = false;

        foreach (var (stepId, newPosition) in action.Moves)
        {
            if (!stepsBuilder.TryGetValue(stepId, out var step)) continue;
            if (step.Position == newPosition) continue;

            stepsBuilder[stepId] = step.WithPosition(newPosition);
            changed = true;
        }

        if (!changed) return state;

        var updatedDocument = document with { Steps = stepsBuilder.ToImmutable() };
        return state.WithMutation(action.WorkflowId, updatedDocument);
    }

    [ReducerMethod]
    public static EditorState ReduceRemoveStepAction(EditorState state, RemoveStepAction action)
    {
        if (!state.OpenDocuments.TryGetValue(action.WorkflowId, out var document))
            return state;
        if (!document.Steps.ContainsKey(action.StepId)) return state;

        var newSteps = document.Steps.Remove(action.StepId);
        var newLinks = document.Links.Values
            .Where(l => l.SourceNodeId != action.StepId && l.TargetNodeId != action.StepId)
            .ToImmutableDictionary(l => l.Id);

        var updatedDocument = document with { Steps = newSteps, Links = newLinks };
        return state.WithMutation(action.WorkflowId, updatedDocument);
    }

    [ReducerMethod]
    public static EditorState ReduceRenameStepAction(EditorState state, RenameStepAction action)
    {
        if (!state.OpenDocuments.TryGetValue(action.WorkflowId, out var document)) return state;
        if (!document.Steps.TryGetValue(action.StepId, out var step)) return state;
        if (step.Name == action.NewName) return state;

        var updatedDocument = document with
        {
            Steps = document.Steps.SetItem(action.StepId, step.WithName(action.NewName))
        };
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

    [ReducerMethod]
    public static EditorState ReduceUndoAction(EditorState state, UndoAction action)
    {
        if (!state.OpenDocuments.TryGetValue(action.WorkflowId, out var current)) return state;
        if (!state.UndoStacks.TryGetValue(action.WorkflowId, out var undoStack) || undoStack.IsEmpty) return state;

        var previous = undoStack.Peek();
        var newUndo = undoStack.Pop();
        var redoStack = state.RedoStacks.GetValueOrDefault(action.WorkflowId, ImmutableStack<WorkflowDocument>.Empty);

        return state with
        {
            OpenDocuments = state.OpenDocuments.SetItem(action.WorkflowId, previous),
            UndoStacks = newUndo.IsEmpty
                ? state.UndoStacks.Remove(action.WorkflowId)
                : state.UndoStacks.SetItem(action.WorkflowId, newUndo),
            RedoStacks = state.RedoStacks.SetItem(action.WorkflowId, redoStack.Push(current)),
            DirtyDocuments = state.DirtyDocuments.Add(action.WorkflowId)
        };
    }

    [ReducerMethod]
    public static EditorState ReduceRedoAction(EditorState state, RedoAction action)
    {
        if (!state.OpenDocuments.TryGetValue(action.WorkflowId, out var current)) return state;
        if (!state.RedoStacks.TryGetValue(action.WorkflowId, out var redoStack) || redoStack.IsEmpty) return state;

        var next = redoStack.Peek();
        var newRedo = redoStack.Pop();
        var undoStack = state.UndoStacks.GetValueOrDefault(action.WorkflowId, ImmutableStack<WorkflowDocument>.Empty);

        return state with
        {
            OpenDocuments = state.OpenDocuments.SetItem(action.WorkflowId, next),
            RedoStacks = newRedo.IsEmpty
                ? state.RedoStacks.Remove(action.WorkflowId)
                : state.RedoStacks.SetItem(action.WorkflowId, newRedo),
            UndoStacks = state.UndoStacks.SetItem(action.WorkflowId, undoStack.Push(current)),
            DirtyDocuments = state.DirtyDocuments.Add(action.WorkflowId)
        };
    }

    private const int UndoLimit = 50;

    private static EditorState WithMutation(this EditorState state, string workflowId, WorkflowDocument updated)
    {
        var previousUndo = state.UndoStacks.GetValueOrDefault(workflowId, ImmutableStack<WorkflowDocument>.Empty);
        var snapshot = state.OpenDocuments.GetValueOrDefault(workflowId);
        var newUndo = snapshot is null
            ? previousUndo
            : Bound(previousUndo.Push(snapshot), UndoLimit);

        return state with
        {
            OpenDocuments = state.OpenDocuments.SetItem(workflowId, updated),
            DirtyDocuments = state.DirtyDocuments.Add(workflowId),
            UndoStacks = state.UndoStacks.SetItem(workflowId, newUndo),
            RedoStacks = state.RedoStacks.Remove(workflowId)
        };
    }

    private static ImmutableStack<T> Bound<T>(ImmutableStack<T> stack, int limit)
    {
        var count = 0;
        foreach (var _ in stack) count++;
        if (count <= limit) return stack;

        var keep = stack.Take(limit).Reverse().ToList();
        var bounded = ImmutableStack<T>.Empty;
        foreach (var item in keep) bounded = bounded.Push(item);
        return bounded;
    }
}
