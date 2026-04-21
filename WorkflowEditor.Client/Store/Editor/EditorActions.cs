namespace WorkflowEditor.Client.Store.Editor;

using Core.Models;

// Навигация и жизненный цикл вкладок
public record OpenWorkflowAction(WorkflowDocument Document);
public record SwitchTabAction(string WorkflowId);
public record CloseTabAction(string WorkflowId);

// Мутации графа (работают в контексте ActiveDocumentId)
public record AddStepAction(string WorkflowId, WorkflowStep Step);
public record RemoveStepAction(string WorkflowId, string StepId);
public record MoveStepAction(string WorkflowId, string StepId, CanvasPosition NewPosition);

// Инициирует процесс сохранения
public record SaveWorkflowAction(string WorkflowId);

// Результаты выполнения (их будут слушать редьюсеры, чтобы, например, скрыть лоадер)
public record SaveWorkflowSuccessAction(string WorkflowId);
public record SaveWorkflowFailedAction(string WorkflowId, string ErrorMessage);

// Инициация загрузки
public record LoadWorkflowAction(string WorkflowId);

// Успешный результат (передаем уже десериализованный документ)
public record LoadWorkflowSuccessAction(WorkflowDocument Document);

// Ошибка загрузки
public record LoadWorkflowFailedAction(string ErrorMessage);

public record CreateNewWorkflowAction(string TriggerSource = "UI");

// Мутации связей (линков)
public record AddLinkAction(string WorkflowId, WorkflowLink Link);
public record RemoveLinkAction(string WorkflowId, string LinkId);

// Добавь это в EditorActions.cs
public record RenameStepAction(string WorkflowId, string StepId, string NewName);

public record StartEditingStepAction(string StepId);
public record StopEditingStepAction();