using System.Collections.Immutable;
using Fluxor;
using WorkflowEditor.Core.Models;

namespace WorkflowEditor.Client.Store.Editor;

[FeatureState]
public record EditorState
{
    public bool IsLoading { get; init; }

    public ImmutableDictionary<string, WorkflowDocument> OpenDocuments { get; init; }

    public string? ActiveDocumentId { get; init; }

    public string? EditingStepId { get; init; }

    public ClipboardPayload? Clipboard { get; init; }

    // Документы с несохранёнными правками
    public ImmutableHashSet<string> DirtyDocuments { get; init; }

    // Undo/Redo: для каждой вкладки храним стек предыдущих/последующих версий документа
    public ImmutableDictionary<string, ImmutableStack<WorkflowDocument>> UndoStacks { get; init; }

    public ImmutableDictionary<string, ImmutableStack<WorkflowDocument>> RedoStacks { get; init; }

    public EditorState()
    {
        IsLoading = false;
        OpenDocuments = ImmutableDictionary<string, WorkflowDocument>.Empty;
        ActiveDocumentId = null;
        EditingStepId = null;
        Clipboard = null;
        DirtyDocuments = ImmutableHashSet<string>.Empty;
        UndoStacks = ImmutableDictionary<string, ImmutableStack<WorkflowDocument>>.Empty;
        RedoStacks = ImmutableDictionary<string, ImmutableStack<WorkflowDocument>>.Empty;
    }

    public bool IsDirty(string workflowId) => DirtyDocuments.Contains(workflowId);

    public bool CanUndo(string workflowId) =>
        UndoStacks.TryGetValue(workflowId, out var stack) && !stack.IsEmpty;

    public bool CanRedo(string workflowId) =>
        RedoStacks.TryGetValue(workflowId, out var stack) && !stack.IsEmpty;
}

public record ClipboardPayload(
    IReadOnlyList<WorkflowStep> Steps,
    IReadOnlyList<WorkflowLink> Links,
    CanvasPosition Origin);
