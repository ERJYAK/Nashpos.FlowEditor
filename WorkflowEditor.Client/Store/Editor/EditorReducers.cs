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
            RedoStacks = state.RedoStacks.Remove(action.Document.Name),
            // Любой импорт/открытие — также свежий source для всех subflow-узлов с этим именем.
            SubflowCache = state.SubflowCache.SetItem(action.Document.Name, action.Document),
            // Если открытие пришло из batch-импорта — счётчик батча декрементируется.
            PendingImports = Math.Max(0, state.PendingImports - 1),
            // Очистить инвалидные пометки и добавить в порядок вкладок (если ещё нет).
            InvalidStepIds = state.InvalidStepIds.Remove(action.Document.Name),
            TabOrder = state.TabOrder.Contains(action.Document.Name)
                ? state.TabOrder
                : state.TabOrder.Add(action.Document.Name)
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
        var newOrder = state.TabOrder.Remove(action.Name);
        var nextActive = state.ActiveDocumentName == action.Name
            ? newOrder.FirstOrDefault() ?? remaining.Keys.FirstOrDefault()
            : state.ActiveDocumentName;

        return state with
        {
            OpenDocuments = remaining,
            ActiveDocumentName = nextActive,
            DirtyDocuments = state.DirtyDocuments.Remove(action.Name),
            UndoStacks = state.UndoStacks.Remove(action.Name),
            RedoStacks = state.RedoStacks.Remove(action.Name),
            InvalidStepIds = state.InvalidStepIds.Remove(action.Name),
            TabOrder = newOrder
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
            RedoStacks = state.RedoStacks.Remove(action.Document.Name),
            SubflowCache = state.SubflowCache.SetItem(action.Document.Name, action.Document),
            InvalidStepIds = state.InvalidStepIds.Remove(action.Document.Name),
            TabOrder = state.TabOrder.Contains(action.Document.Name)
                ? state.TabOrder
                : state.TabOrder.Add(action.Document.Name)
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
    public static EditorState ReduceUpdateStepBranchesAction(
        EditorState state, UpdateStepBranchesAction action) =>
        ReplaceStep(state, action.Name, action.StepId, step =>
        {
            var newPersistentId = string.IsNullOrWhiteSpace(action.NewPersistentStepId)
                ? null
                : action.NewPersistentStepId.Trim();

            var changed = step.StepId != newPersistentId
                       || !BranchEquals(step.OnSuccess, action.OnSuccess)
                       || !BranchEquals(step.OnFail, action.OnFail)
                       || !BreakpointEquals(step.Breakpoint, action.Breakpoint);
            if (!changed) return null;

            return step switch
            {
                BaseStep b => b with
                {
                    StepId = newPersistentId,
                    OnSuccess = action.OnSuccess,
                    OnFail = action.OnFail,
                    Breakpoint = action.Breakpoint
                },
                SubflowStep s => s with
                {
                    StepId = newPersistentId,
                    OnSuccess = action.OnSuccess,
                    OnFail = action.OnFail,
                    Breakpoint = action.Breakpoint
                },
                _ => step
            };
        });

    private static bool BranchEquals(Branch? a, Branch? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        if (a.Decision != b.Decision) return false;
        if (a.StepId != b.StepId) return false;
        if (a.ErrorCode != b.ErrorCode) return false;
        if (a.ErrorMessage != b.ErrorMessage) return false;
        if (a.Description != b.Description) return false;

        var aWhen = a.WhenCode;
        var bWhen = b.WhenCode;
        if (aWhen is null && bWhen is null) return true;
        if (aWhen is null || bWhen is null) return false;
        if (aWhen.Count != bWhen.Count) return false;
        foreach (var kv in aWhen)
        {
            if (!bWhen.TryGetValue(kv.Key, out var other)) return false;
            if (!BranchEquals(kv.Value, other)) return false;
        }
        return true;
    }

    private static bool BreakpointEquals(BreakpointConfig? a, BreakpointConfig? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        return a.Set == b.Set
            && a.RestoreAtNextStep == b.RestoreAtNextStep
            && a.BreakIteration == b.BreakIteration
            && a.TimeoutMs == b.TimeoutMs;
    }

    [ReducerMethod]
    public static EditorState ReduceUpdateStepContextStringAction(
        EditorState state, UpdateStepContextStringAction action) =>
        ReplaceStep(state, action.Name, action.StepId, step =>
        {
            var strings = step.Context?.Strings ?? ImmutableDictionary<string, string>.Empty;

            if (string.IsNullOrEmpty(action.NewValue))
            {
                if (!strings.ContainsKey(action.Key)) return null;
                strings = strings.Remove(action.Key);
            }
            else
            {
                if (strings.TryGetValue(action.Key, out var existing) && existing == action.NewValue)
                    return null;
                strings = strings.SetItem(action.Key, action.NewValue);
            }

            var newContext = (step.Context ?? new StepContext()) with
            {
                Strings = strings.IsEmpty ? null : strings
            };
            StepContext? finalCtx = newContext.IsEmpty ? null : newContext;

            return step switch
            {
                BaseStep b => b with { Context = finalCtx },
                SubflowStep s => s with { Context = finalCtx },
                _ => step
            };
        });

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

    // --- Copy / Paste ----------------------------------------------------------

    [ReducerMethod]
    public static EditorState ReduceCopySelectionAction(EditorState state, CopySelectionAction action)
    {
        if (action.StepIds.Count == 0) return state;
        if (!state.OpenDocuments.TryGetValue(action.Name, out var editor)) return state;

        var idSet = action.StepIds.ToHashSet();
        var copiedSteps = editor.Document.Steps.Where(s => idSet.Contains(s.Id)).ToImmutableList();
        if (copiedSteps.Count == 0) return state;

        // Только внутренние связи (где обе стороны выделены).
        var copiedLinks = editor.Links.Values
            .Where(l => idSet.Contains(l.SourceStepId) && idSet.Contains(l.TargetStepId))
            .ToImmutableList();

        var positions = copiedSteps
            .Select(s => (s.Id, Pos: editor.NodePositions.GetValueOrDefault(s.Id, new CanvasPosition(0, 0))))
            .ToImmutableDictionary(t => t.Id, t => t.Pos);

        var origin = new CanvasPosition(
            positions.Values.Min(p => p.X),
            positions.Values.Min(p => p.Y));

        return state with { Clipboard = new ClipboardPayload(copiedSteps, copiedLinks, positions, origin) };
    }

    [ReducerMethod]
    public static EditorState ReducePasteClipboardAction(EditorState state, PasteClipboardAction action)
    {
        if (state.Clipboard is not { } clip) return state;
        if (clip.Steps.Count == 0) return state;
        if (!state.OpenDocuments.TryGetValue(action.Name, out var editor)) return state;

        // old Id → new step (с новым Id, через CloneAsNew).
        var idMap = new Dictionary<string, WorkflowStep>(clip.Steps.Count);
        var positionsBuilder = editor.NodePositions.ToBuilder();
        var stepsBuilder = editor.Document.Steps.ToBuilder();

        foreach (var oldStep in clip.Steps)
        {
            var newStep = oldStep.CloneAsNew();
            idMap[oldStep.Id] = newStep;
            stepsBuilder.Add(newStep);

            var oldPos = clip.Positions.GetValueOrDefault(oldStep.Id, new CanvasPosition(0, 0));
            var newPos = new CanvasPosition(
                action.CanvasX + (oldPos.X - clip.Origin.X),
                action.CanvasY + (oldPos.Y - clip.Origin.Y));
            positionsBuilder[newStep.Id] = newPos;
        }

        var linksBuilder = editor.Links.ToBuilder();
        foreach (var oldLink in clip.Links)
        {
            if (!idMap.TryGetValue(oldLink.SourceStepId, out var newSrc)) continue;
            if (!idMap.TryGetValue(oldLink.TargetStepId, out var newTgt)) continue;

            var newId = Guid.NewGuid().ToString();
            linksBuilder[newId] = new EditorLink
            {
                Id = newId,
                SourceStepId = newSrc.Id,
                TargetStepId = newTgt.Id
            };
        }

        var updated = editor with
        {
            Document = editor.Document with { Steps = stepsBuilder.ToImmutable() },
            Links = linksBuilder.ToImmutable(),
            NodePositions = positionsBuilder.ToImmutable()
        };
        return state.WithMutation(action.Name, updated);
    }

    // --- Batch-импорт ---------------------------------------------------------

    [ReducerMethod]
    public static EditorState ReduceBatchImportStartedAction(EditorState state, BatchImportStartedAction action) =>
        state with { PendingImports = state.PendingImports + Math.Max(0, action.Count) };

    // ImportFileFailed декрементит счётчик (файл обработан с ошибкой);
    // ReduceOpenWorkflowAction делает то же при успешном импорте.
    [ReducerMethod]
    public static EditorState ReduceImportFileFailedAction(EditorState state, ImportFileFailedAction _) =>
        state with { PendingImports = Math.Max(0, state.PendingImports - 1) };

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

    // --- Reorder / Rename / Validation ----------------------------------------

    [ReducerMethod]
    public static EditorState ReduceReorderTabsAction(EditorState state, ReorderTabsAction action)
    {
        // Берём только имена, которые реально открыты — игнорируем мусор от JS.
        var sanitized = action.NewOrder
            .Where(state.OpenDocuments.ContainsKey)
            .Distinct()
            .ToImmutableList();
        // Дописываем «забытые» открытые имена в конец, чтобы порядок остался полным.
        foreach (var existing in state.OpenDocuments.Keys)
            if (!sanitized.Contains(existing)) sanitized = sanitized.Add(existing);
        return state with { TabOrder = sanitized };
    }

    [ReducerMethod]
    public static EditorState ReduceMarkInvalidStepsAction(EditorState state, MarkInvalidStepsAction action)
    {
        var set = action.StepIds.ToImmutableHashSet();
        return state with { InvalidStepIds = state.InvalidStepIds.SetItem(action.Name, set) };
    }

    [ReducerMethod]
    public static EditorState ReduceClearInvalidStepsAction(EditorState state, ClearInvalidStepsAction action) =>
        state with { InvalidStepIds = state.InvalidStepIds.Remove(action.Name) };

    [ReducerMethod]
    public static EditorState ReduceCascadeRenameSubflowReferencesAction(
        EditorState state, CascadeRenameSubflowReferencesAction action)
    {
        if (action.OldSubflowName == action.NewSubflowName) return state;

        var docsBuilder = state.OpenDocuments.ToBuilder();
        foreach (var (name, editor) in state.OpenDocuments)
        {
            var changed = false;
            var newSteps = editor.Document.Steps;
            for (var i = 0; i < newSteps.Count; i++)
            {
                if (newSteps[i] is SubflowStep s && s.SubflowName == action.OldSubflowName)
                {
                    newSteps = newSteps.SetItem(i, s.WithSubflowName(action.NewSubflowName));
                    changed = true;
                }
            }
            if (changed)
                docsBuilder[name] = editor with { Document = editor.Document with { Steps = newSteps } };
        }
        return state with { OpenDocuments = docsBuilder.ToImmutable() };
    }

    [ReducerMethod]
    public static EditorState ReduceRenameWorkflowAction(EditorState state, RenameWorkflowAction action)
    {
        if (action.OldName == action.NewName) return state;
        if (!state.OpenDocuments.TryGetValue(action.OldName, out var oldEditor)) return state;
        if (state.OpenDocuments.ContainsKey(action.NewName)) return state; // конфликт ловит effect

        // 1. Перенос самой записи под новый ключ + смена Document.Name.
        var renamedDoc = oldEditor.Document with { Name = action.NewName };
        var renamedEditor = oldEditor with { Document = renamedDoc };
        var openDocs = state.OpenDocuments.Remove(action.OldName).SetItem(action.NewName, renamedEditor);

        var dirty = state.DirtyDocuments.Contains(action.OldName)
            ? state.DirtyDocuments.Remove(action.OldName).Add(action.NewName)
            : state.DirtyDocuments;

        var undo = state.UndoStacks.TryGetValue(action.OldName, out var undoStack)
            ? state.UndoStacks.Remove(action.OldName).SetItem(action.NewName, undoStack)
            : state.UndoStacks;
        var redo = state.RedoStacks.TryGetValue(action.OldName, out var redoStack)
            ? state.RedoStacks.Remove(action.OldName).SetItem(action.NewName, redoStack)
            : state.RedoStacks;

        var subflowCache = state.SubflowCache;
        if (subflowCache.TryGetValue(action.OldName, out var cached))
            subflowCache = subflowCache.Remove(action.OldName)
                                       .SetItem(action.NewName, cached with { Name = action.NewName });

        var invalid = state.InvalidStepIds.TryGetValue(action.OldName, out var invalidSet)
            ? state.InvalidStepIds.Remove(action.OldName).SetItem(action.NewName, invalidSet)
            : state.InvalidStepIds;

        var tabOrder = state.TabOrder.Replace(action.OldName, action.NewName);
        var active = state.ActiveDocumentName == action.OldName ? action.NewName : state.ActiveDocumentName;

        // 2. Cascade: переименовать ВСЕ SubflowStep с OldName на NewName во всех документах.
        if (action.CascadeSubflows)
        {
            var docsBuilder = openDocs.ToBuilder();
            foreach (var (name, editor) in openDocs)
            {
                var changed = false;
                var newSteps = editor.Document.Steps;
                for (var i = 0; i < newSteps.Count; i++)
                {
                    if (newSteps[i] is SubflowStep s && s.SubflowName == action.OldName)
                    {
                        newSteps = newSteps.SetItem(i, s.WithSubflowName(action.NewName));
                        changed = true;
                    }
                }
                if (changed)
                {
                    docsBuilder[name] = editor with { Document = editor.Document with { Steps = newSteps } };
                }
            }
            openDocs = docsBuilder.ToImmutable();
        }

        return state with
        {
            OpenDocuments = openDocs,
            ActiveDocumentName = active,
            DirtyDocuments = dirty,
            UndoStacks = undo,
            RedoStacks = redo,
            SubflowCache = subflowCache,
            InvalidStepIds = invalid,
            TabOrder = tabOrder
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
            RedoStacks = state.RedoStacks.Remove(name),
            // Любая правка обнуляет ранее зафиксированный набор невалидных узлов —
            // граф снова считается потенциально валидным до следующего save.
            InvalidStepIds = state.InvalidStepIds.Remove(name)
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
