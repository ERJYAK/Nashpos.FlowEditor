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

// Properties-панель -------------------------------------------------------------
public sealed record StartEditingStepAction(string StepId);
public sealed record StopEditingStepAction;

// Undo / Redo на уровне документа ----------------------------------------------
public sealed record UndoAction(string Name);
public sealed record RedoAction(string Name);
