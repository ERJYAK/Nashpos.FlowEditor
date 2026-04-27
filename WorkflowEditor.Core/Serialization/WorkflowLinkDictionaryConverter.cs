using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using WorkflowEditor.Core.Models;

namespace WorkflowEditor.Core.Serialization;

public sealed class WorkflowLinkDictionaryConverter : JsonConverter<ImmutableDictionary<string, WorkflowLink>>
{
    public override ImmutableDictionary<string, WorkflowLink> Read(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var list = JsonSerializer.Deserialize<List<WorkflowLink>>(ref reader, options);
        if (list is null) return ImmutableDictionary<string, WorkflowLink>.Empty;

        var builder = ImmutableDictionary.CreateBuilder<string, WorkflowLink>();
        foreach (var link in list)
        {
            builder[link.Id] = link;
        }
        return builder.ToImmutable();
    }

    public override void Write(
        Utf8JsonWriter writer, ImmutableDictionary<string, WorkflowLink> value, JsonSerializerOptions options)
    {
        var ordered = value.Values.OrderBy(l => l.Id, StringComparer.Ordinal).ToList();
        JsonSerializer.Serialize(writer, ordered, options);
    }
}
