using System.Collections.Immutable;
using WorkflowEditor.Client.Store.Editor;
using WorkflowEditor.Core.Models;

namespace WorkflowEditor.Client.Services.Topology;

// Строит UI-слой связей и виртуальных STOP-узлов из бизнес-модели Branch'ей.
//
// Source of truth — `WorkflowStep.OnSuccess/OnFail/Breakpoint` в `WorkflowDocument`.
// `EditorLink` и `StopNodeInfo` — производные от него, восстанавливаются на импорте
// и пересобираются при правке через `UpdateStepBranchesAction`.
//
// Правила построения:
//   - У шага без явных onSuccess/onFail — одна линейная связь к step[i+1] (Kind=Default).
//   - С явными ветками: каждая ветка → отдельная связь со своим Kind/Decision.
//     - NEXT_STEP без stepId → step[i+1] (если есть).
//     - GOTO_STEP → шаг с persistent StepId == branch.StepId.
//     - BREAK_WORKFLOW / SILENT_BREAK_WORKFLOW → виртуальный STOP-узел.
//   - Каждая запись `onFail.whenCode[code]` — отдельная связь Kind=WhenCode со своим кодом.
public static class BranchLinkBuilder
{
    public sealed record BuildResult(
        ImmutableDictionary<string, EditorLink> Links,
        ImmutableDictionary<string, StopNodeInfo> VirtualStops);

    public static BuildResult Build(IReadOnlyList<WorkflowStep> steps)
    {
        var links = ImmutableDictionary.CreateBuilder<string, EditorLink>();
        var stops = ImmutableDictionary.CreateBuilder<string, StopNodeInfo>();

        var byPersistentId = steps
            .Where(s => !string.IsNullOrEmpty(s.StepId))
            .GroupBy(s => s.StepId!)
            .ToDictionary(g => g.Key, g => g.First().Id);

        for (var i = 0; i < steps.Count; i++)
            AppendForStep(steps, i, byPersistentId, links, stops);

        return new BuildResult(links.ToImmutable(), stops.ToImmutable());
    }

    // Пересобирает связи и STOP-узлы только для одного шага, оставляя остальное нетронутым.
    // Используется после UpdateStepBranchesAction.
    public static (ImmutableDictionary<string, EditorLink> Links,
                   ImmutableDictionary<string, StopNodeInfo> Stops)
        RebuildForStep(
            string stepId,
            IReadOnlyList<WorkflowStep> steps,
            ImmutableDictionary<string, EditorLink> existingLinks,
            ImmutableDictionary<string, StopNodeInfo> existingStops)
    {
        // Снимаем все исходящие связи редактируемого шага и его STOP-узлы.
        var linksBuilder = existingLinks
            .Where(kv => kv.Value.SourceStepId != stepId)
            .ToImmutableDictionary()
            .ToBuilder();
        var stopsBuilder = existingStops
            .Where(kv => kv.Value.OwnerStepId != stepId)
            .ToImmutableDictionary()
            .ToBuilder();

        var index = -1;
        for (var i = 0; i < steps.Count; i++)
            if (steps[i].Id == stepId) { index = i; break; }
        if (index < 0)
            return (linksBuilder.ToImmutable(), stopsBuilder.ToImmutable());

        var byPersistentId = steps
            .Where(s => !string.IsNullOrEmpty(s.StepId))
            .GroupBy(s => s.StepId!)
            .ToDictionary(g => g.Key, g => g.First().Id);

        AppendForStep(steps, index, byPersistentId, linksBuilder, stopsBuilder);

        return (linksBuilder.ToImmutable(), stopsBuilder.ToImmutable());
    }

    private static void AppendForStep(
        IReadOnlyList<WorkflowStep> steps,
        int index,
        IReadOnlyDictionary<string, string> byPersistentId,
        ImmutableDictionary<string, EditorLink>.Builder links,
        ImmutableDictionary<string, StopNodeInfo>.Builder stops)
    {
        var step = steps[index];
        string? linearNextId = index + 1 < steps.Count ? steps[index + 1].Id : null;

        if (step.OnSuccess is null && step.OnFail is null)
        {
            if (linearNextId is not null)
            {
                AddLink(links, step.Id, linearNextId, EditorLinkKind.Default, Decision.NextStep);
            }
            return;
        }

        if (step.OnSuccess is { } os)
            AppendBranch(step.Id, EditorLinkKind.OnSuccess, os, null, linearNextId, byPersistentId, links, stops);

        if (step.OnFail is { } of)
        {
            AppendBranch(step.Id, EditorLinkKind.OnFail, of, null, linearNextId, byPersistentId, links, stops);

            if (of.WhenCode is { } when)
            {
                foreach (var (code, branch) in when)
                    AppendBranch(step.Id, EditorLinkKind.WhenCode, branch, code, linearNextId,
                        byPersistentId, links, stops);
            }
        }
    }

    private static void AppendBranch(
        string sourceStepId,
        EditorLinkKind kind,
        Branch branch,
        int? whenCode,
        string? linearNextId,
        IReadOnlyDictionary<string, string> byPersistentId,
        ImmutableDictionary<string, EditorLink>.Builder links,
        ImmutableDictionary<string, StopNodeInfo>.Builder stops)
    {
        switch (branch.Decision)
        {
            case Decision.NextStep:
                if (linearNextId is not null)
                    AddLink(links, sourceStepId, linearNextId, kind, branch.Decision, whenCode);
                return;

            case Decision.GotoStep:
                if (!string.IsNullOrEmpty(branch.StepId)
                    && byPersistentId.TryGetValue(branch.StepId, out var targetId))
                {
                    AddLink(links, sourceStepId, targetId, kind, branch.Decision, whenCode);
                }
                return;

            case Decision.BreakWorkflow:
            case Decision.SilentBreakWorkflow:
                var stopId = StopId(sourceStepId, kind, whenCode);
                stops[stopId] = new StopNodeInfo
                {
                    Id = stopId,
                    OwnerStepId = sourceStepId,
                    BranchKind = kind,
                    WhenCode = whenCode,
                    IsSilent = branch.Decision == Decision.SilentBreakWorkflow,
                    ErrorCode = branch.Decision == Decision.BreakWorkflow ? branch.ErrorCode : null,
                    ErrorMessage = branch.Decision == Decision.BreakWorkflow ? branch.ErrorMessage : null
                };
                AddLink(links, sourceStepId, stopId, kind, branch.Decision, whenCode,
                    errorCode: branch.Decision == Decision.BreakWorkflow ? branch.ErrorCode : null,
                    errorMessage: branch.Decision == Decision.BreakWorkflow ? branch.ErrorMessage : null);
                return;
        }
    }

    private static void AddLink(
        ImmutableDictionary<string, EditorLink>.Builder links,
        string source, string target,
        EditorLinkKind kind,
        Decision decision,
        int? whenCode = null,
        int? errorCode = null,
        string? errorMessage = null)
    {
        var id = Guid.NewGuid().ToString();
        links[id] = new EditorLink
        {
            Id = id,
            SourceStepId = source,
            TargetStepId = target,
            Kind = kind,
            Decision = decision,
            WhenCode = whenCode,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
    }

    public static string StopId(string sourceStepId, EditorLinkKind kind, int? whenCode) =>
        whenCode is null
            ? $"_stop_{sourceStepId}_{kind}"
            : $"_stop_{sourceStepId}_{kind}_{whenCode}";

    public static bool IsStopId(string id) => id.StartsWith("_stop_", StringComparison.Ordinal);
}
