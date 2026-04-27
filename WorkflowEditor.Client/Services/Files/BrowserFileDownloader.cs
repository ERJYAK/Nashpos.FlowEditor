using Microsoft.JSInterop;

namespace WorkflowEditor.Client.Services.Files;

public sealed class BrowserFileDownloader(IJSRuntime js) : IFileDownloader
{
    public Task DownloadAsync(string fileName, string content, string mimeType = "application/json") =>
        js.InvokeVoidAsync("fileDownload.save", fileName, content, mimeType).AsTask();
}
