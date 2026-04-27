using System.Collections.Immutable;
using WorkflowEditor.Core.Models;

namespace WorkflowEditor.Client.Services.Layout;

// Раскладывает шаги вертикальной цепочкой top-down. Используется при импорте/загрузке,
// когда у шагов нет ассоциированных позиций (бизнес-формат их не хранит).
public static class LinearAutoLayout
{
    public const double StepX = 0;
    public const double StepY = 0;
    public const double StepStride = 120;

    public static ImmutableDictionary<string, CanvasPosition> ForSteps(IReadOnlyList<WorkflowStep> steps)
    {
        var builder = ImmutableDictionary.CreateBuilder<string, CanvasPosition>();
        for (var i = 0; i < steps.Count; i++)
        {
            builder[steps[i].Id] = new CanvasPosition(StepX, StepY + i * StepStride);
        }
        return builder.ToImmutable();
    }
}
