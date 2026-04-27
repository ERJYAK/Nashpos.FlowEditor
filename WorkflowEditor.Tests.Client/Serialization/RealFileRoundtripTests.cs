using System.Text.Json;
using WorkflowEditor.Core.Models;
using WorkflowEditor.Core.Serialization;

namespace WorkflowEditor.Tests.Client.Serialization;

// Главный контрактный тест: каждый из реальных production-файлов в TestFlows/
// должен парситься, сериализоваться обратно и быть семантически идентичен оригиналу.
// Whitespace/отступы игнорируются (сравнение через JsonElement.DeepEquals).
public class RealFileRoundtripTests
{
    public static IEnumerable<object[]> RealFiles()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "TestFlows");
        if (!Directory.Exists(dir)) yield break;
        foreach (var path in Directory.GetFiles(dir, "*.json"))
        {
            yield return [Path.GetFileName(path)];
        }
    }

    [Theory]
    [MemberData(nameof(RealFiles))]
    public void Roundtrips_to_semantically_equivalent_json(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestFlows", fileName);
        var original = File.ReadAllText(path);
        var options = JsonConfiguration.GetOptions();

        var doc = JsonSerializer.Deserialize<WorkflowDocument>(original, options);
        doc.Should().NotBeNull($"file {fileName} must deserialize to a WorkflowDocument");

        var roundtripped = JsonSerializer.Serialize(doc, options);

        using var originalDoc = JsonDocument.Parse(original);
        using var roundDoc = JsonDocument.Parse(roundtripped);

        var equal = JsonElement.DeepEquals(originalDoc.RootElement, roundDoc.RootElement);
        equal.Should().BeTrue($"""
            file {fileName} must roundtrip without semantic changes.
            Got:
            {roundtripped}
            """);
    }

    [Fact]
    public void TestFlows_directory_is_not_empty()
    {
        // Защита от молчаливо-пустого MemberData (если bin не получит TestFlows/ — выше тесты молча 0 шт.).
        RealFiles().Should().NotBeEmpty("TestFlows must be deployed to test output directory");
    }
}
