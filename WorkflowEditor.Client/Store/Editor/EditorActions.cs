using WorkflowEditor.Core.Models;

namespace WorkflowEditor.Client.Store.Editor;

// Жизненный цикл вкладок --------------------------------------------------------
public sealed record OpenWorkflowAction(WorkflowDocument Document);
public sealed record SwitchTabAction(string Name);
public sealed record CloseTabAction(string Name);

// Восстановить ранее закрытую вкладку: возвращает её в OpenDocuments из ClosedDocuments
// со всем расположением узлов/связей. No-op, если запись отсутствует или такое имя
// уже открыто (защита от гонок).
public sealed record RestoreClosedDocumentAction(string Name);

// Создание нового процесса — сначала сигнал «пользователь нажал кнопку и ввёл имя»,
// эффект делает черновой WorkflowDocument и открывает его.
public sealed record CreateWorkflowRequestedAction(string Name);

// Импорт JSON-файла (drag-drop / file-picker) -----------------------------------
public sealed record ImportFileRequestedAction(string FileName, string Payload);
public sealed record ImportFileFailedAction(string FileName, string ErrorMessage);

// Двойной клик по subflow-узлу: открыть из сессионного кэша, иначе — пустой черновик.
public sealed record OpenSubflowAction(string Name);

// Мутации шагов -----------------------------------------------------------------
public sealed record AddStepAction(string Name, WorkflowStep Step, CanvasPosition Position);
public sealed record RemoveStepsAction(string Name, IReadOnlyList<string> StepIds);
public sealed record UpdateStepDescriptionAction(string Name, string StepId, string NewDescription);
public sealed record UpdateBaseStepKindAction(string Name, string StepId, string NewStepKind);
public sealed record UpdateSubflowNameAction(string Name, string StepId, string NewSubflowName);
public sealed record MoveStepAction(string Name, string StepId, CanvasPosition NewPosition);

// Точечный апдейт строкового значения в `Context.Strings[Key]`.
// Используется, например, для сохранения JS-скрипта (`script`) у `execute-js-script`.
// Если NewValue пуст — ключ удаляется из словаря.
public sealed record UpdateStepContextStringAction(string Name, string StepId, string Key, string NewValue);

// Применить изменения веток (onSuccess/onFail) и брейкпоинта к шагу.
// `NewPersistentStepId == null` ⇒ persistent stepId сбрасывается;
// `OnSuccess`/`OnFail`/`Breakpoint` == null ⇒ соответствующие поля сбрасываются.
public sealed record UpdateStepBranchesAction(
    string Name,
    string StepId,
    string? NewPersistentStepId,
    Branch? OnSuccess,
    Branch? OnFail,
    BreakpointConfig? Breakpoint);

// Связи (UI-only) ---------------------------------------------------------------
public sealed record AddLinkAction(string Name, EditorLink Link);
public sealed record RemoveLinksAction(string Name, IReadOnlyList<string> LinkIds);

// Copy/Paste --------------------------------------------------------------------
public sealed record CopySelectionAction(string Name, IReadOnlyList<string> StepIds);
public sealed record PasteClipboardAction(string Name, double CanvasX, double CanvasY);

// Batch-импорт нескольких файлов одновременно (drag-drop) -----------------------
// Started(N) ставит счётчик; каждый успешный OpenWorkflow / ImportFileFailed —
// декремент в соответствующем reducer'е. Пока счётчик > 0, MainLayout подавляет
// subflow-not-found snackbar (следующий файл может оказаться искомым subflow).
public sealed record BatchImportStartedAction(int Count);

// Переименование вкладки (= workflow). Cascade=true — также переименовывает
// все SubflowStep с OldName во всех открытых документах. Конфликт-чек делает effect.
public sealed record RenameWorkflowRequestedAction(string OldName, string NewName, bool CascadeSubflows);
public sealed record RenameWorkflowAction(string OldName, string NewName, bool CascadeSubflows);
public sealed record RenameWorkflowFailedAction(string OldName, string NewName, string ErrorMessage);

// ПКМ → «Переименовать subflow». Cascade=true → делегирует на RenameWorkflow либо
// CascadeRenameSubflowReferences (если открытой вкладки нет).
// Cascade=false → только этот узел через UpdateSubflowName.
public sealed record RenameSubflowRequestedAction(string DocName, string StepId, string NewSubflowName, bool Cascade);

// Cascade-переименование SubflowStep по всем открытым документам, БЕЗ переноса вкладки.
public sealed record CascadeRenameSubflowReferencesAction(string OldSubflowName, string NewSubflowName);

// Drag-reorder вкладок (массив имён в новом порядке).
public sealed record ReorderTabsAction(IReadOnlyList<string> NewOrder);

// Подсветка невалидных узлов на холсте (при provoked save с невалидным графом).
public sealed record MarkInvalidStepsAction(string Name, IReadOnlyList<string> StepIds);
public sealed record ClearInvalidStepsAction(string Name);

// Properties-панель -------------------------------------------------------------
public sealed record StartEditingStepAction(string StepId);
public sealed record StopEditingStepAction;

// Undo / Redo на уровне документа ----------------------------------------------
public sealed record UndoAction(string Name);
public sealed record RedoAction(string Name);
