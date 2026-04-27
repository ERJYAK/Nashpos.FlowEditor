using System.Collections.Immutable;
using WorkflowEditor.Client.Store.Editor;
using WorkflowEditor.Core.Models;

namespace WorkflowEditor.Client.Services.Topology;

// Линейный резолвер порядка шагов: граф (steps + ориентированные ссылки) должен быть линейной
// цепочкой — каждый узел имеет ≤1 входящую и ≤1 исходящую связь, ровно один «head» (узел без
// входящих). Возвращает шаги в порядке цепочки или объясняет, что именно сломано.
public static class StepOrderResolver
{
    public sealed record Result(bool IsSuccess, ImmutableList<WorkflowStep>? Ordered, string? ErrorMessage)
    {
        public static Result Success(ImmutableList<WorkflowStep> ordered) => new(true, ordered, null);
        public static Result Failure(string message) => new(false, null, message);
    }

    public static Result Resolve(IReadOnlyList<WorkflowStep> steps, IReadOnlyDictionary<string, EditorLink> links)
    {
        if (steps.Count == 0) return Result.Success(ImmutableList<WorkflowStep>.Empty);
        if (steps.Count == 1) return Result.Success(steps.ToImmutableList());

        var stepsById = steps.ToDictionary(s => s.Id);
        var inDegree = steps.ToDictionary(s => s.Id, _ => 0);
        var outNext = new Dictionary<string, string>(steps.Count);

        foreach (var link in links.Values)
        {
            if (!stepsById.ContainsKey(link.SourceStepId) || !stepsById.ContainsKey(link.TargetStepId))
                return Result.Failure("связь ссылается на несуществующий шаг");

            if (outNext.ContainsKey(link.SourceStepId))
                return Result.Failure($"у шага «{Describe(stepsById[link.SourceStepId])}» больше одной исходящей связи");
            outNext[link.SourceStepId] = link.TargetStepId;

            inDegree[link.TargetStepId]++;
            if (inDegree[link.TargetStepId] > 1)
                return Result.Failure($"у шага «{Describe(stepsById[link.TargetStepId])}» больше одной входящей связи");
        }

        var heads = inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key).ToList();
        if (heads.Count == 0)
            return Result.Failure("в графе нет начального шага (есть цикл)");
        if (heads.Count > 1)
            return Result.Failure($"в графе несколько несвязанных цепочек ({heads.Count}). Соедините шаги в одну цепочку");

        var ordered = ImmutableList.CreateBuilder<WorkflowStep>();
        var current = heads[0];
        var visited = new HashSet<string>();
        while (true)
        {
            if (!visited.Add(current))
                return Result.Failure("в графе обнаружен цикл");
            ordered.Add(stepsById[current]);
            if (!outNext.TryGetValue(current, out var next)) break;
            current = next;
        }

        if (ordered.Count != steps.Count)
            return Result.Failure($"шагов в графе {steps.Count}, в цепочке {ordered.Count} — есть оторванные узлы");

        return Result.Success(ordered.ToImmutable());
    }

    private static string Describe(WorkflowStep step) => step switch
    {
        Core.Models.Steps.BaseStep b => string.IsNullOrEmpty(b.StepKind) ? step.Description : b.StepKind,
        Core.Models.Steps.SubflowStep s => string.IsNullOrEmpty(s.SubflowName) ? step.Description : s.SubflowName,
        _ => step.Description
    };
}
