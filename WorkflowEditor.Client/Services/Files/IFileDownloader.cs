namespace WorkflowEditor.Client.Services.Files;

public interface IFileDownloader
{
    Task DownloadAsync(string fileName, string content, string mimeType = "application/json");
}
