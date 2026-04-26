namespace WorkflowEditor.Core.Serialization;

using System.Text.Json;
using System.Text.Json.Serialization;

public static class JsonConfiguration
{
    public static JsonSerializerOptions GetOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,

            // Полиморфизм: разрешаем найти поле "type" в любом месте объекта
            AllowOutOfOrderMetadataProperties = true,

            // Защита от зацикливаний, если в будущем появятся ссылки
            ReferenceHandler = ReferenceHandler.IgnoreCycles
        };
    }
}
