namespace WorkflowEditor.Core.Models.Steps;

// Шаг-ссылка на другой workflow — соответствует `{ "subflow": "<name>" }` в JSON-формате.
// `SubflowName` — имя другого процесса (= имя файла = ключ хранения), которое разрешается
// при выполнении и при отображении в редакторе.
public sealed record SubflowStep : WorkflowStep
{
    public string SubflowName { get; init; } = string.Empty;

    public SubflowStep WithSubflowName(string subflowName) => this with { SubflowName = subflowName };

    public override WorkflowStep WithDescription(string description) =>
        this with { Description = description };

    public override WorkflowStep CloneAsNew() =>
        this with { Id = Guid.NewGuid().ToString() };
}
