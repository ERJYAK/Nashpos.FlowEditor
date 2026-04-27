using WorkflowEditor.Api.Interceptors;
using WorkflowEditor.Api.Services;
using WorkflowEditor.Application;
using WorkflowEditor.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc(o => o.Interceptors.Add<ExceptionInterceptor>());
builder.Services.AddScoped<ExceptionInterceptor>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(o => o.AddPolicy("WorkflowEditor", corsBuilder =>
{
    if (corsOrigins.Length == 0)
    {
        corsBuilder.AllowAnyOrigin();
    }
    else
    {
        corsBuilder.WithOrigins(corsOrigins).AllowCredentials();
    }

    corsBuilder.AllowAnyMethod()
        .AllowAnyHeader()
        .WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding");
}));

var app = builder.Build();

app.UseCors("WorkflowEditor");
app.UseGrpcWeb();
app.MapGrpcService<WorkflowStorageService>().EnableGrpcWeb();
app.MapGet("/", () => "gRPC Server is running.");
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

public partial class Program;
