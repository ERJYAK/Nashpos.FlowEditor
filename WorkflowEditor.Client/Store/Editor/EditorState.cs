using System.Collections.Immutable;
using Fluxor;
using WorkflowEditor.Core.Models;

namespace WorkflowEditor.Client.Store.Editor;

[FeatureState]
public sealed record EditorState
{
    public bool IsLoading { get; init; }

    // Открытые в редакторе документы. Ключ — Name (он же ключ хранения и таргет subflow).
    public ImmutableDictionary<string, EditorDocument> OpenDocuments { get; init; } =
        ImmutableDictionary<string, EditorDocument>.Empty;

    public string? ActiveDocumentName { get; init; }

    public string? EditingStepId { get; init; }

    // Кэш чужих процессов — для отображения вложенных шагов в SubflowNodeWidget.
    public ImmutableDictionary<string, WorkflowDocument> SubflowCache { get; init; } =
        ImmutableDictionary<string, WorkflowDocument>.Empty;

    // Имена subflow, которые сейчас грузятся, — антишторм против повторных fetch-ей.
    public ImmutableHashSet<string> LoadingSubflows { get; init; } =
        ImmutableHashSet<string>.Empty;

    public ImmutableHashSet<string> DirtyDocuments { get; init; } =
        ImmutableHashSet<string>.Empty;

    public ImmutableDictionary<string, ImmutableStack<EditorDocument>> UndoStacks { get; init; } =
        ImmutableDictionary<string, ImmutableStack<EditorDocument>>.Empty;

    public ImmutableDictionary<string, ImmutableStack<EditorDocument>> RedoStacks { get; init; } =
        ImmutableDictionary<string, ImmutableStack<EditorDocument>>.Empty;

    public bool IsDirty(string name) => DirtyDocuments.Contains(name);

    public bool CanUndo(string name) =>
        UndoStacks.TryGetValue(name, out var stack) && !stack.IsEmpty;

    public bool CanRedo(string name) =>
        RedoStacks.TryGetValue(name, out var stack) && !stack.IsEmpty;
}

// Открытый в редакторе документ: бизнес-данные + UI-only слой (визуальные связи + позиции узлов).
// Ни Links, ни NodePositions не уезжают на сервер — это разметка холста, существующая только
// в браузерной сессии. Сервер видит только `Document.Steps` (порядок = семантика).
public sealed record EditorDocument
{
    public required WorkflowDocument Document { get; init; }
    public ImmutableDictionary<string, EditorLink> Links { get; init; } =
        ImmutableDictionary<string, EditorLink>.Empty;
    public ImmutableDictionary<string, CanvasPosition> NodePositions { get; init; } =
        ImmutableDictionary<string, CanvasPosition>.Empty;
}

// Связь между двумя узлами на холсте. Ориентированная: source → target.
public sealed record EditorLink
{
    public required string Id { get; init; }
    public required string SourceStepId { get; init; }
    public required string TargetStepId { get; init; }
}
