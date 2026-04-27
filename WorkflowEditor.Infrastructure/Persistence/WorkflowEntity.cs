namespace WorkflowEditor.Infrastructure.Persistence;

internal sealed class WorkflowEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public int Version { get; set; }
}
