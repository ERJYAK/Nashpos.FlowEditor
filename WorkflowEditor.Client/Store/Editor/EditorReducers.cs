using System.Collections.Immutable;
using Fluxor;
using WorkflowEditor.Client.Services.Layout;
using WorkflowEditor.Core.Models;
using WorkflowEditor.Core.Models.Steps;

namespace WorkflowEditor.Client.Store.Editor;

public static class EditorReducers
{
    private const int UndoLimit = 50;

    // --- Жизненный цикл вкладок -----------------------------------------------

    [ReducerMethod]
    public static EditorState ReduceOpenWorkflowAction(EditorState state, OpenWorkflowAction action)
    {
        var editor = NewEditorDocument(action.Document);
        return state with
        {
            OpenDocuments = state.OpenDocuments.SetItem(action.Document.Name, editor),
            ActiveDocumentName = action.Document.Name,
            DirtyDocuments = state.DirtyDocuments.Remove(action.Document.Name),
            UndoStacks = state.UndoStacks.Remove(action.Document.Name),
            RedoStacks = state.RedoStacks.Remove(action.Document.Name)
        };
    }

    [ReducerMethod]
    public static EditorState ReduceSwitchTabAction(EditorState state, SwitchTabAction action) =>
        state with { ActiveDocumentName = action.Name };

    [ReducerMethod]
    public static EditorState ReduceCloseTabAction(EditorState state, CloseTabAction action)
    {
        if (!state.OpenDocuments.ContainsKey(action.Name)) return state;

        var remaining = state.OpenDocuments.Remove(action.Name);
        var nextActive = state.ActiveDocumentName == action.Name
            ? remaining.Keys.FirstOrDefault()
            : state.ActiveDocumentName;

        return state with
        {
            OpenDocuments = remaining,
            ActiveDocumentName = nextActive,
            DirtyDocuments = state.DirtyDocuments.Remove(action.Name),
            UndoStacks = state.UndoStacks.Remove(action.Name),
            RedoStacks = state.RedoStacks.Remove(action.Name)
        };
    }

    // --- Загрузка/сохранение --------------------------------------------------

    [ReducerMethod]
    public static EditorState ReduceLoadWorkflowAction(EditorState state, LoadWorkflowAction _) =>
        state with { IsLoading = true };

    [ReducerMethod]
    public static EditorState ReduceLoadWorkflowSuccessAction(EditorState state, LoadWorkflowSuccessAction action)
    {
        var editor = NewEditorDocument(action.Document);
        return state with
        {
            IsLoading = false,
            OpenDocuments = state.OpenDocuments.SetItem(action.Document.Name, editor),
            ActiveDocumentName = action.Document.Name,
            DirtyDocuments = state.DirtyDocuments.Remove(action.Document.Name),
            UndoStacks = state.UndoStacks.Remove(action.Document.Name),
            RedoStacks = state.RedoStacks.Remove(action.Document.Name)
        };
    }

    [ReducerMethod]
    public static EditorState ReduceLoadWorkflowFailedAction(EditorState state, LoadWorkflowFailedAction _) =>
        state with { IsLoading = false };

    [ReducerMethod]
    public static EditorState ReduceSaveWorkflowSuccessAction(EditorState state, SaveWorkflowSuccessAction action) =>
        state with
        {
            DirtyDocuments = state.DirtyDocuments.Remove(action.Name),
            // Любое сохранение инвалидирует кэш, чтобы subflow-узлы перезагрузили актуальные шаги.
            SubflowCache = state.SubflowCache.Remove(action.Name)
        };

    // --- Subflow-кэш ----------------------------------------------------------

    [ReducerMethod]
    public static EditorState ReduceLoadSubflowAction(EditorState state, LoadSubflowAction action) =>
        state with { LoadingSubflows = state.LoadingSubflows.Add(action.Name) };

    [ReducerMethod]
    public static EditorState ReduceLoadSubflowSuccessAction(EditorState state, LoadSubflowSuccessAction action) =>
        state with
        {
            SubflowCache = state.SubflowCache.SetItem(action.Name, action.Document),
            LoadingSubflows = state.LoadingSubflows.Remove(action.Name)
        };

    [ReducerMethod]
    public static EditorState ReduceLoadSubflowFailedAction(EditorState state, LoadSubflowFailedAction action) =>
        state with { LoadingSubflows = state.LoadingSubflows.Remove(action.Name) };

    // --- Мутации шагов --------------------------------------------------------

    [ReducerMethod]
    public static EditorState ReduceAddStepAction(EditorState state, AddStepAction action)
    {
        if (!state.OpenDocuments.TryGetValue(action.Name, out var editor)) return state;
        if (editor.Document.Steps.Any(s => s.Id == action.Step.Id)) return state;

        var updated = editor with
        {
            Document = editor.Document with { Steps = editor.Document.Steps.Add(action.Step) },
            NodePositions = editor.NodePositions.SetItem(action.Step.Id, action.Position)
        };
        return state.WithMutation(action.Name, updated);
    }

    [ReducerMethod]
    public static EditorState ReduceRemoveStepsAction(EditorState state, RemoveStepsAction action)
    {
        if (action.StepIds.Count == 0) return state;
        if (!state.OpenDocuments.TryGetValue(action.Name, out var editor)) return state;

        var idSet = action.StepIds.ToHashSet();
        var newSteps = editor.Document.Steps.RemoveAll(s => idSet.Contains(s.Id));
        if (newSteps.Count == editor.Document.Steps.Count) return state;

        var newLinks = editor.Links
            .Where(l => !idSet.Contains(l.Value.SourceStepId) && !idSet.Contains(l.Value.TargetStepId))
            .ToImmutableDictionary(kv => kv.Key, kv => kv.Value);

        var newPositions = editor.NodePositions.RemoveRange(action.StepIds);

        var updated = editor with
        {
            Document = editor.Document with { Steps = newSteps },
            Links = newLinks,
            NodePositions = newPositions
        };
        return state.WithMutation(action.Name, updated);
    }

    [ReducerMethod]
    public static EditorState ReduceUpdateStepDescriptionAction(EditorState state, UpdateStepDescriptionAction action) =>
        ReplaceStep(state, action.Name, action.StepId, step =>
            step.Description == action.NewDescription
                ? null
                : step.WithDescription(action.NewDescription));

    [ReducerMethod]
    public static EditorState ReduceUpdateBaseStepKindAction(EditorState state, UpdateBaseStepKindAction action) =>
        ReplaceStep(state, action.Name, action.StepId, step => step is BaseStep b && b.StepKind != action.NewStepKind
            ? b.WithStepKind(action.NewStepKind)
            : null);

    [ReducerMethod]
    public static EditorState ReduceUpdateSubflowNameAction(EditorState state, UpdateSubflowNameAction action) =>
        ReplaceStep(state, action.Name, action.StepId, step => step is SubflowStep s && s.SubflowName != action.NewSubflowName
            ? s.WithSubflowName(action.NewSubflowName)
            : null);

    [ReducerMethod]
    public static EditorState ReduceMoveStepAction(EditorState state, MoveStepAction action)
    {
        if (!state.OpenDocuments.TryGetValue(action.Name, out var editor)) return state;
        if (editor.NodePositions.TryGetValue(action.StepId, out var current) && current == action.NewPosition)
            return state;

        // Move не пушит undo-snapshot и не помечает dirty: позиции — UI-only, не уезжают на сервер.
        var updated = editor with { NodePositions = editor.NodePositions.SetItem(action.StepId, action.NewPosition) };
        return state with { OpenDocuments = state.OpenDocuments.SetItem(action.Name, updated) };
    }

    // --- Связи -----------------------------------------------------------------

    [ReducerMethod]
    public static EditorState ReduceAddLinkAction(EditorState state, AddLinkAction action)
    {
        if (!state.OpenDocuments.TryGetValue(action.Name, out var editor)) return state;
        if (editor.Links.ContainsKey(action.Link.Id)) return state;

        var updated = editor with { Links = editor.Links.SetItem(action.Link.Id, action.Link) };
        return state.WithMutation(action.Name, updated);
    }

    [ReducerMethod]
    public static EditorState ReduceRemoveLinksAction(EditorState state, RemoveLinksAction action)
    {
        if (action.LinkIds.Count == 0) return state;
        if (!state.OpenDocuments.TryGetValue(action.Name, out var editor)) return state;

        var newLinks = editor.Links.RemoveRange(action.LinkIds);
        if (newLinks.Count == editor.Links.Count) return state;

        return state.WithMutation(action.Name, editor with { Links = newLinks });
    }

    // --- Properties-панель -----------------------------------------------------

    [ReducerMethod]
    public static EditorState ReduceStartEditingStepAction(EditorState state, StartEditingStepAction action) =>
        state with { EditingStepId = action.StepId };

    [ReducerMethod]
    public static EditorState ReduceStopEditingStepAction(EditorState state, StopEditingStepAction _) =>
        state with { EditingStepId = null };

    // --- Undo / Redo -----------------------------------------------------------

    [ReducerMethod]
    public static EditorState ReduceUndoAction(EditorState state, UndoAction action)
    {
        if (!state.OpenDocuments.TryGetValue(action.Name, out var current)) return state;
        if (!state.UndoStacks.TryGetValue(action.Name, out var undo) || undo.IsEmpty) return state;

        var previous = undo.Peek();
        var newUndo = undo.Pop();
        var redo = state.RedoStacks.GetValueOrDefault(action.Name, ImmutableStack<EditorDocument>.Empty);

        return state with
        {
            OpenDocuments = state.OpenDocuments.SetItem(action.Name, previous),
            UndoStacks = newUndo.IsEmpty ? state.UndoStacks.Remove(action.Name) : state.UndoStacks.SetItem(action.Name, newUndo),
            RedoStacks = state.RedoStacks.SetItem(action.Name, redo.Push(current)),
            DirtyDocuments = state.DirtyDocuments.Add(action.Name)
        };
    }

    [ReducerMethod]
    public static EditorState ReduceRedoAction(EditorState state, RedoAction action)
    {
        if (!state.OpenDocuments.TryGetValue(action.Name, out var current)) return state;
        if (!state.RedoStacks.TryGetValue(action.Name, out var redo) || redo.IsEmpty) return state;

        var next = redo.Peek();
        var newRedo = redo.Pop();
        var undo = state.UndoStacks.GetValueOrDefault(action.Name, ImmutableStack<EditorDocument>.Empty);

        return state with
        {
            OpenDocuments = state.OpenDocuments.SetItem(action.Name, next),
            RedoStacks = newRedo.IsEmpty ? state.RedoStacks.Remove(action.Name) : state.RedoStacks.SetItem(action.Name, newRedo),
            UndoStacks = state.UndoStacks.SetItem(action.Name, undo.Push(current)),
            DirtyDocuments = state.DirtyDocuments.Add(action.Name)
        };
    }

    // --- Helpers ---------------------------------------------------------------

    private static EditorDocument NewEditorDocument(WorkflowDocument document) =>
        new()
        {
            Document = document,
            Links = LinearLinks(document.Steps),
            NodePositions = LinearAutoLayout.ForSteps(document.Steps)
        };

    // Линейный граф: step[i] → step[i+1].
    private static ImmutableDictionary<string, EditorLink> LinearLinks(IReadOnlyList<WorkflowStep> steps)
    {
        if (steps.Count < 2) return ImmutableDictionary<string, EditorLink>.Empty;

        var builder = ImmutableDictionary.CreateBuilder<string, EditorLink>();
        for (var i = 0; i < steps.Count - 1; i++)
        {
            var id = Guid.NewGuid().ToString();
            builder[id] = new EditorLink { Id = id, SourceStepId = steps[i].Id, TargetStepId = steps[i + 1].Id };
        }
        return builder.ToImmutable();
    }

    private static EditorState ReplaceStep(
        EditorState state, string name, string stepId, Func<WorkflowStep, WorkflowStep?> mutate)
    {
        if (!state.OpenDocuments.TryGetValue(name, out var editor)) return state;
        var index = editor.Document.Steps.FindIndex(s => s.Id == stepId);
        if (index < 0) return state;

        var mutated = mutate(editor.Document.Steps[index]);
        if (mutated is null) return state;

        var updatedSteps = editor.Document.Steps.SetItem(index, mutated);
        var updated = editor with { Document = editor.Document with { Steps = updatedSteps } };
        return state.WithMutation(name, updated);
    }

    private static EditorState WithMutation(this EditorState state, string name, EditorDocument updated)
    {
        var snapshot = state.OpenDocuments.GetValueOrDefault(name);
        var previousUndo = state.UndoStacks.GetValueOrDefault(name, ImmutableStack<EditorDocument>.Empty);
        var newUndo = snapshot is null ? previousUndo : Bound(previousUndo.Push(snapshot), UndoLimit);

        return state with
        {
            OpenDocuments = state.OpenDocuments.SetItem(name, updated),
            DirtyDocuments = state.DirtyDocuments.Add(name),
            UndoStacks = state.UndoStacks.SetItem(name, newUndo),
            RedoStacks = state.RedoStacks.Remove(name)
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
