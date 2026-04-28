using Blazor.Diagrams.Core.Models;
using WorkflowEditor.Core.Models;
using WorkflowEditor.Core.Models.Steps;

namespace WorkflowEditor.Client.Diagram.Nodes;

// Узел для шага `execute-js-script`. Это специализация BaseStep с константным StepKind,
// поэтому модель структурно идентична BaseNodeModel — отдельный тип нужен только чтобы
// зарегистрировать в Diagram свой widget (JsScriptNodeWidget).
public sealed class JsScriptNodeModel : WorkflowNodeModel
{
    public const string ConstStepKind = "execute-js-script";

    public JsScriptNodeModel(BaseStep step, CanvasPosition position) : base(step, position)
    {
        AddPort(PortAlignment.Top);
        AddPort(PortAlignment.Bottom);
        AddPort(PortAlignment.Left);
        AddPort(PortAlignment.Right);
    }
}
