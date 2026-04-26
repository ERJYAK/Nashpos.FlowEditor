namespace WorkflowEditor.Client.Services.Api;

public enum ApiOutcome
{
    Success,
    NotFound,
    ValidationError,
    NetworkError,
    ServerError
}

public sealed record ApiResult<T>(ApiOutcome Outcome, T? Value, string? ErrorMessage)
{
    public bool IsSuccess => Outcome == ApiOutcome.Success;

    public static ApiResult<T> Success(T value) => new(ApiOutcome.Success, value, null);
    public static ApiResult<T> NotFound() => new(ApiOutcome.NotFound, default, null);
    public static ApiResult<T> Validation(string message) => new(ApiOutcome.ValidationError, default, message);
    public static ApiResult<T> Network(string message) => new(ApiOutcome.NetworkError, default, message);
    public static ApiResult<T> ServerError(string message) => new(ApiOutcome.ServerError, default, message);
}

public readonly record struct Unit
{
    public static Unit Value => default;
}
