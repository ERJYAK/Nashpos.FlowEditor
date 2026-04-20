namespace WorkflowEditor.Core.Serialization;

using System.Text.Json;
using System.Text.Json.Serialization;

public static class JsonConfigurationL
{
    public static JsonSerializerOptions GetOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true, // Для красивого форматирования, если нужно смотреть глазами
            
            // Критически важная настройка для полиморфизма:
            // Позволяет парсеру искать поле 'type' в любом месте объекта, а не только первым
            AllowOutOfOrderMetadataProperties = true,
            
            // Защита от зацикливаний, если в будущем появятся ссылки
            ReferenceHandler = ReferenceHandler.IgnoreCycles 
        };
    }
}