using System.Text.Json;
using System.Text.Json.Serialization;

namespace WorkflowEditor.Core.Serialization;

public static class JsonConfiguration
{
    public static JsonSerializerOptions GetOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            ReferenceHandler = ReferenceHandler.IgnoreCycles
        };

        options.Converters.Add(new WorkflowStepJsonConverter());

        return options;
    }
}
