using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using WorkflowEditor.Application.Workflows.Delete;
using WorkflowEditor.Application.Workflows.Export;
using WorkflowEditor.Application.Workflows.Get;
using WorkflowEditor.Application.Workflows.Import;
using WorkflowEditor.Application.Workflows.List;
using WorkflowEditor.Application.Workflows.Save;

namespace WorkflowEditor.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IGetWorkflowQueryHandler, GetWorkflowQueryHandler>();
        services.AddScoped<ISaveWorkflowCommandHandler, SaveWorkflowCommandHandler>();
        services.AddScoped<IListWorkflowsQueryHandler, ListWorkflowsQueryHandler>();
        services.AddScoped<IDeleteWorkflowCommandHandler, DeleteWorkflowCommandHandler>();
        services.AddScoped<IImportWorkflowJsonCommandHandler, ImportWorkflowJsonCommandHandler>();
        services.AddScoped<IExportWorkflowJsonQueryHandler, ExportWorkflowJsonQueryHandler>();

        services.AddSingleton<IWorkflowDocumentJsonMigrator, IdentityWorkflowDocumentJsonMigrator>();

        services.AddValidatorsFromAssemblyContaining<SaveWorkflowValidator>();

        return services;
    }
}
