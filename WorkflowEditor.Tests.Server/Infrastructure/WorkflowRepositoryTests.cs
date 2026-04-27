using WorkflowEditor.Core.Models.Steps;
using WorkflowEditor.Infrastructure.Persistence;
using WorkflowEditor.Tests.Server.TestKit;

namespace WorkflowEditor.Tests.Server.Infrastructure;

public class WorkflowRepositoryTests
{
    private static WorkflowRepository NewRepo(out AppDbContext db)
    {
        db = WorkflowFactory.NewInMemoryContext();
        return new WorkflowRepository(db, TimeProvider.System);
    }

    [Fact]
    public async Task Upsert_then_Get_roundtrips_the_document()
    {
        var repo = NewRepo(out var db);
        var doc = WorkflowFactory.Document("import", "Import flow",
            WorkflowFactory.Sub("prepare-import", "Prepare", iterate: true),
            WorkflowFactory.Base("apply-import", "Apply"));

        await repo.UpsertAsync(doc, CancellationToken.None);
        var loaded = await repo.GetAsync("import", CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.Name.Should().Be("import");
        loaded.Description.Should().Be("Import flow");
        loaded.Steps.Should().HaveCount(2);
        loaded.Steps[0].Should().BeOfType<SubflowStep>().Which.Iterate.Should().BeTrue();
        loaded.Steps[1].Should().BeOfType<BaseStep>().Which.StepKind.Should().Be("apply-import");

        await db.DisposeAsync();
    }

    [Fact]
    public async Task Upsert_increments_Version_for_existing_workflow()
    {
        var repo = NewRepo(out var db);
        var doc = WorkflowFactory.Document("import", steps: WorkflowFactory.Base("apply-import"));
        await repo.UpsertAsync(doc, CancellationToken.None);
        await repo.UpsertAsync(doc with { Description = "updated" }, CancellationToken.None);

        var entity = db.ChangeTracker.Entries<WorkflowEntity>().Single().Entity;
        entity.Version.Should().Be(2);
        entity.Description.Should().Be("updated");

        await db.DisposeAsync();
    }

    [Fact]
    public async Task Delete_returns_true_for_existing_and_false_for_missing()
    {
        var repo = NewRepo(out var db);
        await repo.UpsertAsync(WorkflowFactory.Document("import", steps: WorkflowFactory.Base("k")),
            CancellationToken.None);

        (await repo.DeleteAsync("import", CancellationToken.None)).Should().BeTrue();
        (await repo.DeleteAsync("import", CancellationToken.None)).Should().BeFalse();

        await db.DisposeAsync();
    }

    [Fact]
    public async Task List_returns_summaries_ordered_by_name()
    {
        var repo = NewRepo(out var db);
        await repo.UpsertAsync(WorkflowFactory.Document("zeta", "z", WorkflowFactory.Base("k")), CancellationToken.None);
        await repo.UpsertAsync(WorkflowFactory.Document("alpha", "a", WorkflowFactory.Base("k")), CancellationToken.None);

        var items = await repo.ListAsync(CancellationToken.None);

        items.Select(i => i.Name).Should().ContainInOrder("alpha", "zeta");
        items[0].Description.Should().Be("a");

        await db.DisposeAsync();
    }
}
