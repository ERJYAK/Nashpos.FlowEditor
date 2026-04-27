using Grpc.Core;
using Grpc.Core.Interceptors;

namespace WorkflowEditor.Api.Interceptors;

public sealed class ExceptionInterceptor(ILogger<ExceptionInterceptor> logger) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(request, context);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
            throw new RpcException(new Status(StatusCode.Cancelled, "request was cancelled"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "unhandled exception in {Method}", context.Method);
            throw new RpcException(new Status(StatusCode.Internal, "internal server error"));
        }
    }
}
