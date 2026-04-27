using WorkflowEditor.Core.Models;

namespace WorkflowEditor.Client.Store.Editor;

// Жизненный цикл вкладок --------------------------------------------------------
public sealed record OpenWorkflowAction(WorkflowDocument Document);
public sealed record SwitchTabAction(string Name);
public sealed record CloseTabAction(string Name);

// Создание нового процесса — сначала сигнал «пользователь нажал кнопку и ввёл имя»,
// эффект делает черновой WorkflowDocument и открывает его.
public sealed record CreateWorkflowRequestedAction(string Name);

// Загрузка с сервера ------------------------------------------------------------
public sealed record LoadWorkflowAction(string Name);
public sealed record LoadWorkflowSuccessAction(WorkflowDocument Document);
public sealed record LoadWorkflowFailedAction(string Name, string ErrorMessage);

// Сохранение --------------------------------------------------------------------
public sealed record SaveWorkflowAction(string Name);
public sealed record SaveWorkflowSuccessAction(string Name);
public sealed record SaveWorkflowFailedAction(string Name, string ErrorMessage);

// Импорт JSON-файла (drag-drop / file-picker) -----------------------------------
public sealed record ImportFileRequestedAction(string FileName, string Payload);
public sealed record ImportFileFailedAction(string FileName, string ErrorMessage);

// Lazy-fetch для отображения subflow внутри узла --------------------------------
public sealed record LoadSubflowAction(string Name);
public sealed record LoadSubflowSuccessAction(string Name, WorkflowDocument Document);
public sealed record LoadSubflowFailedAction(string Name, string ErrorMessage);

// Двойной клик по subflow-узлу: «попробуй загрузить, если нет — открой пустой черновик».
public sealed record OpenSubflowAction(string Name);

// Мутации шагов -----------------------------------------------------------------
public sealed record AddStepAction(string Name, WorkflowStep Step, CanvasPosition Position);
public sealed record RemoveStepsAction(string Name, IReadOnlyList<string> StepIds);
public sealed record UpdateStepDescriptionAction(string Name, string StepId, string NewDescription);
public sealed record UpdateBaseStepKindAction(string Name, string StepId, string NewStepKind);
public sealed record UpdateSubflowNameAction(string Name, string StepId, string NewSubflowName);
public sealed record MoveStepAction(string Name, string StepId, CanvasPosition NewPosition);

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
