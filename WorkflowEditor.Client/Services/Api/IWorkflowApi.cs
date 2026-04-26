using WorkflowEditor.Core.Models;

namespace WorkflowEditor.Client.Services.Api;

public interface IWorkflowApi
{
    Task<ApiResult<WorkflowDocument>> GetAsync(string workflowId, CancellationToken cancellationToken = default);

    Task<ApiResult<Unit>> SaveAsync(WorkflowDocument document, CancellationToken cancellationToken = default);
}
