using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkflowEditor.Application.Abstractions;
using WorkflowEditor.Infrastructure.Persistence;

namespace WorkflowEditor.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var databaseName = configuration["Database:InMemoryName"] ?? "workflows";
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IWorkflowRepository, WorkflowRepository>();

        return services;
    }
}
