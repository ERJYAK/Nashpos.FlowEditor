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

    // Буфер обмена (in-session). Не уезжает на сервер, не персистится.
    public ClipboardPayload? Clipboard { get; init; }

    // Сколько файлов batch-импорта ещё в обработке. Пока > 0, MainLayout
    // подавляет subflow-not-found snackbar (следующий файл может оказаться искомым subflow).
    public int PendingImports { get; init; }

    // Невалидные узлы по документу (для красной обводки). Очищаются на любой mutation
    // через WithMutation — после правки граф снова считается валидным до следующего save.
    public ImmutableDictionary<string, ImmutableHashSet<string>> InvalidStepIds { get; init; } =
        ImmutableDictionary<string, ImmutableHashSet<string>>.Empty;

    // Порядок вкладок (приоритет над OrderBy в Editor). Ключи синхронизируются с
    // OpenDocuments через ReduceOpenWorkflowAction / ReduceCloseTabAction.
    public ImmutableList<string> TabOrder { get; init; } = ImmutableList<string>.Empty;

    public bool IsDirty(string name) => DirtyDocuments.Contains(name);

    public bool CanUndo(string name) =>
        UndoStacks.TryGetValue(name, out var stack) && !stack.IsEmpty;

    public bool CanRedo(string name) =>
        RedoStacks.TryGetValue(name, out var stack) && !stack.IsEmpty;

    public ImmutableHashSet<string> InvalidStepsFor(string name) =>
        InvalidStepIds.GetValueOrDefault(name, ImmutableHashSet<string>.Empty);
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
//
// Условные шаги порождают несколько исходящих связей с разными `Kind`:
//   - `Default` — обычная линейная связь (нет явного onSuccess/onFail).
//   - `OnSuccess` / `OnFail` — основные ветви условия.
//   - `WhenCode` — запись внутри `onFail.whenCode`; ключ кода — в `WhenCode`.
//
// `Decision` определяет, что делает целевой узел:
//   - `NextStep` / `GotoStep` — обычный переход (target — реальный шаг).
//   - `BreakWorkflow` / `SilentBreakWorkflow` — связь идёт в виртуальный STOP-узел;
//     `ErrorCode` / `ErrorMessage` относятся только к `BreakWorkflow`.
public sealed record EditorLink
{
    public required string Id { get; init; }
    public required string SourceStepId { get; init; }
    public required string TargetStepId { get; init; }
    public EditorLinkKind Kind { get; init; } = EditorLinkKind.Default;
    public Decision Decision { get; init; } = Decision.NextStep;
    public int? WhenCode { get; init; }
    public int? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
}

public enum EditorLinkKind
{
    Default,
    OnSuccess,
    OnFail,
    WhenCode
}

// Снимок выделенных узлов и их внутренних связей. Origin = bounding-box top-left:
// при вставке клон шага получает позицию (cursor.X + (oldPos.X - origin.X), …).
public sealed record ClipboardPayload(
    ImmutableList<WorkflowStep> Steps,
    ImmutableList<EditorLink> Links,
    ImmutableDictionary<string, CanvasPosition> Positions,
    CanvasPosition Origin);
