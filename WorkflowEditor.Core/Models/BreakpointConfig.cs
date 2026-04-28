namespace WorkflowEditor.Core.Models;

// Конфигурация брейкпоинта на шаге. В JSON разворачивается в плоские поля шага:
//   "setBreakpoint": true,
//   "restoreAtNextStep": true,
//   "breakIteration": false,
//   "breakPointTimeout": 30000
// Сериализация выполняется в `WorkflowStepJsonConverter` напрямую (плоский слой).
public sealed record BreakpointConfig
{
    // Корневой признак. Все остальные поля имеют смысл только при Set == true.
    public bool Set { get; init; }

    public bool? RestoreAtNextStep { get; init; }

    public bool? BreakIteration { get; init; }

    // Таймаут в миллисекундах.
    public int? TimeoutMs { get; init; }

    public bool IsEmpty =>
        !Set && RestoreAtNextStep is null && BreakIteration is null && TimeoutMs is null;
}
