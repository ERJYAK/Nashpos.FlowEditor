using System.Text.Encodings.Web;
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
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            // По умолчанию System.Text.Json экранирует всё не-ASCII в \uXXXX — кириллица в
            // описаниях и символы (<, >, &) в JS-скриптах превращаются в нечитаемые escape'ы.
            // Relaxed-энкодер пишет их как есть. Файл скачивается/хранится как JSON (не вставляется
            // в HTML), поэтому HTML-экранирование здесь не нужно.
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        options.Converters.Add(new WorkflowStepJsonConverter());

        return options;
    }
}
