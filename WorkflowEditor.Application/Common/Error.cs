namespace WorkflowEditor.Application.Common;

public enum ErrorKind
{
    NotFound,
    Validation,
    Conflict,
    Unexpected
}

public sealed record Error(ErrorKind Kind, string Message, IReadOnlyDictionary<string, string[]>? Failures = null)
{
    public static Error NotFound(string message) => new(ErrorKind.NotFound, message);
    public static Error Validation(string message, IReadOnlyDictionary<string, string[]> failures) =>
        new(ErrorKind.Validation, message, failures);
    public static Error Conflict(string message) => new(ErrorKind.Conflict, message);
    public static Error Unexpected(string message) => new(ErrorKind.Unexpected, message);
}
