using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using WorkflowEditor.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();

// Настраиваем CORS для gRPC-Web
builder.Services.AddCors(o => o.AddPolicy("AllowAll", corsBuilder =>
{
    corsBuilder.AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader()
        .WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding");
}));

var app = builder.Build();

app.UseCors("AllowAll");

app.UseGrpcWeb();

app.MapGrpcService<WorkflowStorageService>().EnableGrpcWeb();

app.MapGet("/", () => "gRPC Server is running.");

app.Run();