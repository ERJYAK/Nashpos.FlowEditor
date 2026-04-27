namespace WorkflowEditor.Application.Workflows.Import;

// Точка расширения для приведения старых JSON-форматов к актуальной схеме.
// Сейчас актуальная схема одна — реализация по умолчанию возвращает payload без изменений.
// При появлении breaking-изменений в JsonConfiguration / WorkflowDocument добавляем
// миграцию здесь и описываем её в `project_json_format_quirks.md`.
public interface IWorkflowDocumentJsonMigrator
{
    string MigrateToCurrentSchema(string jsonPayload);
}

public sealed class IdentityWorkflowDocumentJsonMigrator : IWorkflowDocumentJsonMigrator
{
    public string MigrateToCurrentSchema(string jsonPayload) => jsonPayload;
}
