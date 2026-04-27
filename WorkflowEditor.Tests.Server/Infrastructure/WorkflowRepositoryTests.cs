using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using WorkflowEditor.Core.Models;
using WorkflowEditor.Core.Models.Steps;
using WorkflowEditor.Infrastructure.Persistence;

namespace WorkflowEditor.Tests.Server.Infrastructure;

public class WorkflowRepositoryTests
{
    private static (AppDbContext, WorkflowRepository) NewRepository(TimeProvider? clock = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        return (db, new WorkflowRepository(db, clock ?? TimeProvider.System));
    }

    private static WorkflowDocument Document(string id, string name = "test") => new()
    {
        WorkflowId = id,
        Name = name,
        Steps = new WorkflowStep[]
        {
            new BaseStep { Id = "s-1", Name = "task" }
        }.ToImmutableDictionary(s => s.Id)
    };

    [Fact]
    public async Task GetAsync_returns_null_when_workflow_is_missing()
    {
        var (db, repo) = NewRepository();
        await using var _ = db;

        var result = await repo.GetAsync("missing", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpsertAsync_persists_document_then_GetAsync_returns_it()
    {
        var (db, repo) = NewRepository();
        await using var _ = db;
        var doc = Document("wf-1", "first");

        await repo.UpsertAsync(doc, CancellationToken.None);
        var loaded = await repo.GetAsync("wf-1", CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.WorkflowId.Should().Be("wf-1");
        loaded.Name.Should().Be("first");
        loaded.Steps.Values.Should().ContainSingle().Which.Should().BeOfType<BaseStep>();
    }

    [Fact]
    public async Task UpsertAsync_updates_existing_document_and_bumps_version()
    {
        var (db, repo) = NewRepository();
        await using var _ = db;

        await repo.UpsertAsync(Document("wf-1", "first"), CancellationToken.None);
        await repo.UpsertAsync(Document("wf-1", "second"), CancellationToken.None);

        var entity = await db.Workflows.AsNoTracking().SingleAsync();
        entity.Name.Should().Be("second");
        entity.Version.Should().Be(2);
    }

    [Fact]
    public async Task DeleteAsync_returns_false_for_missing_workflow_and_true_when_removed()
    {
        var (db, repo) = NewRepository();
        await using var _ = db;
        await repo.UpsertAsync(Document("wf-1"), CancellationToken.None);

        var deletedMissing = await repo.DeleteAsync("ghost", CancellationToken.None);
        var deletedExisting = await repo.DeleteAsync("wf-1", CancellationToken.None);

        deletedMissing.Should().BeFalse();
        deletedExisting.Should().BeTrue();
        (await repo.GetAsync("wf-1", CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task ListAsync_returns_summaries_ordered_by_name()
    {
        var (db, repo) = NewRepository();
        await using var _ = db;
        await repo.UpsertAsync(Document("wf-b", "Beta"), CancellationToken.None);
        await repo.UpsertAsync(Document("wf-a", "Alpha"), CancellationToken.None);

        var summaries = await repo.ListAsync(CancellationToken.None);

        summaries.Select(s => s.Name).Should().ContainInOrder("Alpha", "Beta");
    }
}
