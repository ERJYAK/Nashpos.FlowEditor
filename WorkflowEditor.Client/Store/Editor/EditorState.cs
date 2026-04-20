namespace WorkflowEditor.Client.Store.Editor;

using System.Collections.Immutable;
using Fluxor;
using Core.Models;

[FeatureState]
public record EditorState
{
    public bool IsLoading { get; init; }
    
    // Словарь открытых вкладок: WorkflowId -> Document
    public ImmutableDictionary<string, WorkflowDocument> OpenDocuments { get; init; }
    
    public string? ActiveDocumentId { get; init; }

    // Требование Fluxor: конструктор без параметров для начального состояния
    public EditorState()
    {
        IsLoading = false;
        OpenDocuments = ImmutableDictionary<string, WorkflowDocument>.Empty;
        ActiveDocumentId = null;
    }
}