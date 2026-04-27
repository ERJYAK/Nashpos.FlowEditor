using Grpc.Core;
using WorkflowEditor.Application.Common;

namespace WorkflowEditor.Api.Grpc;

internal static class GrpcResultMapper
{
    public static RpcException ToRpcException(this Error error) => error.Kind switch
    {
        ErrorKind.NotFound => new RpcException(new Status(StatusCode.NotFound, error.Message)),
        ErrorKind.Validation => new RpcException(new Status(StatusCode.InvalidArgument, FormatValidation(error))),
        ErrorKind.Conflict => new RpcException(new Status(StatusCode.FailedPrecondition, error.Message)),
        _ => new RpcException(new Status(StatusCode.Internal, error.Message))
    };

    private static string FormatValidation(Error error)
    {
        if (error.Failures is null || error.Failures.Count == 0) return error.Message;

        var details = string.Join("; ", error.Failures
            .Select(kvp => $"{kvp.Key}: {string.Join(", ", kvp.Value)}"));
        return $"{error.Message}: {details}";
    }
}
