using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WorkflowEditor.Core.Models;
using WorkflowEditor.Core.Models.Steps;
using WorkflowEditor.Infrastructure.Persistence;

namespace WorkflowEditor.Tests.Server.TestKit;

internal static class WorkflowFactory
{
    public static WorkflowDocument Document(
        string name,
        string description = "test description",
        params WorkflowStep[] steps) => new()
        {
            Name = name,
            Description = description,
            Steps = steps.Length == 0
                ? ImmutableList<WorkflowStep>.Empty
                : steps.ToImmutableList()
        };

    public static BaseStep Base(string stepKind, string description = "") =>
        new() { StepKind = stepKind, Description = description };

    public static SubflowStep Sub(string subflowName, string description = "", bool? iterate = null) =>
        new() { SubflowName = subflowName, Description = description, Iterate = iterate };

    public static StepContext Context(
        IReadOnlyDictionary<string, string>? strings = null,
        IReadOnlyDictionary<string, long>? integers = null,
        IReadOnlyDictionary<string, JsonElement>? objects = null) => new()
        {
            Strings = strings is null ? null : ImmutableDictionary.CreateRange(strings),
            Integers = integers is null ? null : ImmutableDictionary.CreateRange(integers),
            Objects = objects is null ? null : ImmutableDictionary.CreateRange(objects)
        };

    public static AppDbContext NewInMemoryContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
