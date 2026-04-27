using System.Collections.Immutable;
using WorkflowEditor.Client.Store.Editor;
using WorkflowEditor.Core.Models;

namespace WorkflowEditor.Client.Services.Topology;

// Валидация графа на экспорт: ровно 1 узел "start" (in=0, out=1), ровно 1 "end" (in=1, out=0),
// все остальные — in=1 и out=1. На нарушение возвращает читаемое сообщение и набор виновных
// StepId для красной обводки на холсте. Используется в Editor.SaveActiveAsFile.
public static class WorkflowGraphValidator
{
    public sealed record Result(bool IsValid, string Message, ImmutableHashSet<string> InvalidStepIds)
    {
        public static Result Valid() => new(true, string.Empty, ImmutableHashSet<string>.Empty);
        public static Result Fail(string message, IEnumerable<string> ids) =>
            new(false, message, ids.ToImmutableHashSet());
    }

    public static Result ValidateForExport(IReadOnlyList<WorkflowStep> steps,
                                           IReadOnlyDictionary<string, EditorLink> links)
    {
        if (steps.Count == 0)
            return Result.Fail("Холст пуст — нечего сохранять", []);

        if (steps.Count == 1)
            return Result.Valid();

        var stepIds = steps.Select(s => s.Id).ToHashSet();
        var inDegree = stepIds.ToDictionary(id => id, _ => 0);
        var outDegree = stepIds.ToDictionary(id => id, _ => 0);
        var invalid = new HashSet<string>();

        foreach (var link in links.Values)
        {
            if (!stepIds.Contains(link.SourceStepId) || !stepIds.Contains(link.TargetStepId)) continue;
            outDegree[link.SourceStepId]++;
            inDegree[link.TargetStepId]++;
        }

        var multiIn = stepIds.Where(id => inDegree[id] > 1).ToList();
        var multiOut = stepIds.Where(id => outDegree[id] > 1).ToList();
        var starts = stepIds.Where(id => inDegree[id] == 0).ToList();
        var ends = stepIds.Where(id => outDegree[id] == 0).ToList();
        var orphans = stepIds.Where(id => inDegree[id] == 0 && outDegree[id] == 0).ToList();

        if (multiIn.Count > 0 || multiOut.Count > 0)
        {
            invalid.UnionWith(multiIn);
            invalid.UnionWith(multiOut);
            return Result.Fail(
                "Каждый шаг должен иметь не более одной входящей и одной исходящей связи",
                invalid);
        }

        if (starts.Count != 1)
        {
            invalid.UnionWith(starts);
            return Result.Fail(
                $"В графе должен быть ровно один начальный шаг (без входящих связей), сейчас: {starts.Count}",
                invalid);
        }

        if (ends.Count != 1)
        {
            invalid.UnionWith(ends);
            return Result.Fail(
                $"В графе должен быть ровно один конечный шаг (без исходящих связей), сейчас: {ends.Count}",
                invalid);
        }

        if (orphans.Count > 0)
        {
            invalid.UnionWith(orphans);
            return Result.Fail("На холсте есть оторванные узлы — соедините их в цепочку", invalid);
        }

        return Result.Valid();
    }
}
