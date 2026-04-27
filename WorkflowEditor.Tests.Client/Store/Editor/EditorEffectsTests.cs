using Fluxor;
using NSubstitute;
using WorkflowEditor.Client.Services.Api;
using WorkflowEditor.Client.Store.Editor;
using WorkflowEditor.Core.Models;
using WorkflowEditor.Tests.Client.TestKit;

namespace WorkflowEditor.Tests.Client.Store.Editor;

public class EditorEffectsTests
{
    private static (EditorEffects effects, IDispatcher dispatcher, IWorkflowApi api, EditorState state) Setup(
        EditorState? initial = null)
    {
        var api = Substitute.For<IWorkflowApi>();
        var stateContainer = Substitute.For<IState<EditorState>>();
        var s = initial ?? new EditorState();
        stateContainer.Value.Returns(s);

        var dispatcher = Substitute.For<IDispatcher>();
        var effects = new EditorEffects(api, stateContainer);
        return (effects, dispatcher, api, s);
    }

    [Fact]
    public async Task LoadWorkflow_dispatches_Success_when_api_returns_document()
    {
        var (effects, dispatcher, api, _) = Setup();
        var doc = EditorTestData.Document("import", steps: EditorTestData.Base("k"));
        api.GetAsync("import", Arg.Any<CancellationToken>()).Returns(ApiResult<WorkflowDocument>.Success(doc));

        await effects.HandleLoadWorkflow(new LoadWorkflowAction("import"), dispatcher);

        dispatcher.Received(1).Dispatch(Arg.Is<LoadWorkflowSuccessAction>(a => ReferenceEquals(a.Document, doc)));
    }

    [Fact]
    public async Task LoadWorkflow_dispatches_Failed_with_friendly_message_on_NotFound()
    {
        var (effects, dispatcher, api, _) = Setup();
        api.GetAsync("missing", Arg.Any<CancellationToken>()).Returns(ApiResult<WorkflowDocument>.NotFound());

        await effects.HandleLoadWorkflow(new LoadWorkflowAction("missing"), dispatcher);

        dispatcher.Received(1).Dispatch(Arg.Is<LoadWorkflowFailedAction>(a =>
            a.Name == "missing" && a.ErrorMessage.Contains("missing")));
    }

    [Fact]
    public async Task SaveWorkflow_validates_linearity_and_dispatches_Failed_on_branching_graph()
    {
        var doc = EditorTestData.Document("import", "",
            EditorTestData.Base("a", id: "1"),
            EditorTestData.Base("b", id: "2"),
            EditorTestData.Base("c", id: "3"));
        var initial = EditorReducers.ReduceOpenWorkflowAction(new EditorState(), new OpenWorkflowAction(doc));

        // Заменим линейные links на ветвление 1 → 2, 1 → 3
        var editor = initial.OpenDocuments["import"];
        var branched = editor with
        {
            Links = System.Collections.Immutable.ImmutableDictionary<string, EditorLink>.Empty
                .Add("l1", new EditorLink { Id = "l1", SourceStepId = "1", TargetStepId = "2" })
                .Add("l2", new EditorLink { Id = "l2", SourceStepId = "1", TargetStepId = "3" })
        };
        var withBranch = initial with { OpenDocuments = initial.OpenDocuments.SetItem("import", branched) };

        var (effects, dispatcher, _, _) = Setup(withBranch);

        await effects.HandleSaveWorkflow(new SaveWorkflowAction("import"), dispatcher);

        dispatcher.Received(1).Dispatch(Arg.Is<SaveWorkflowFailedAction>(a => a.Name == "import"));
    }

    [Fact]
    public async Task LoadSubflow_dispatches_Success_and_caches_document()
    {
        var (effects, dispatcher, api, _) = Setup();
        var doc = EditorTestData.Document("prepare-import", steps: EditorTestData.Base("k"));
        api.GetAsync("prepare-import", Arg.Any<CancellationToken>())
            .Returns(ApiResult<WorkflowDocument>.Success(doc));

        await effects.HandleLoadSubflow(new LoadSubflowAction("prepare-import"), dispatcher);

        dispatcher.Received(1).Dispatch(Arg.Is<LoadSubflowSuccessAction>(a =>
            a.Name == "prepare-import" && ReferenceEquals(a.Document, doc)));
    }

    [Fact]
    public Task ImportFileRequested_dispatches_OpenWorkflow_with_filename_as_Name()
    {
        var (effects, dispatcher, _, _) = Setup();
        const string payload = """
            { "description": "Import flow", "steps": [ { "step": "apply-import", "description": "Apply" } ] }
            """;

        return effects.HandleImportFileRequested(
                new ImportFileRequestedAction("import.json", payload), dispatcher)
            .ContinueWith(_ =>
            {
                dispatcher.Received(1).Dispatch(Arg.Is<OpenWorkflowAction>(a =>
                    a.Document.Name == "import" && a.Document.Steps.Count == 1));
            });
    }

    [Fact]
    public async Task ImportFileRequested_dispatches_Failed_on_invalid_json()
    {
        var (effects, dispatcher, _, _) = Setup();

        await effects.HandleImportFileRequested(
            new ImportFileRequestedAction("broken.json", "{ not json"), dispatcher);

        dispatcher.Received(1).Dispatch(Arg.Is<ImportFileFailedAction>(a => a.FileName == "broken.json"));
    }

    [Fact]
    public Task CreateWorkflowRequested_dispatches_OpenWorkflow_with_provided_name()
    {
        var (effects, dispatcher, _, _) = Setup();

        return effects.HandleCreateWorkflowRequested(new CreateWorkflowRequestedAction("new-flow"), dispatcher)
            .ContinueWith(_ => dispatcher.Received(1).Dispatch(Arg.Is<OpenWorkflowAction>(a =>
                a.Document.Name == "new-flow" && a.Document.Steps.Count == 0)));
    }
}
