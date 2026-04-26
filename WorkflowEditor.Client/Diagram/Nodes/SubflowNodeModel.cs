namespace WorkflowEditor.Client.Diagram.Nodes;

using WorkflowEditor.Core.Models.Steps;

public class SubflowNodeModel : WorkflowNodeModel
{
    public string SubflowId { get; }

    public SubflowNodeModel(SubflowStep step) : base(step)
    {
        SubflowId = step.SubflowId;
        // Добавляем порты: один вход слева, один выход справа
        AddPort(Blazor.Diagrams.Core.Models.PortAlignment.Left);
        AddPort(Blazor.Diagrams.Core.Models.PortAlignment.Right);
    }
}