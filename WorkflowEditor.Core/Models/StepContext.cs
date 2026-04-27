using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WorkflowEditor.Core.Models;

public sealed record StepContext
{
    [JsonPropertyName("strings")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ImmutableDictionary<string, string>? Strings { get; init; }

    [JsonPropertyName("integers")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ImmutableDictionary<string, long>? Integers { get; init; }

    [JsonPropertyName("objects")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ImmutableDictionary<string, JsonElement>? Objects { get; init; }

    [JsonIgnore]
    public bool IsEmpty =>
        (Strings is null  || Strings.Count  == 0) &&
        (Integers is null || Integers.Count == 0) &&
        (Objects is null  || Objects.Count  == 0);
}
