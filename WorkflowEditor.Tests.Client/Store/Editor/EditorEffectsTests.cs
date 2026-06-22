using Fluxor;
using NSubstitute;
using WorkflowEditor.Client.Store.Editor;
using WorkflowEditor.Tests.Client.TestKit;

namespace WorkflowEditor.Tests.Client.Store.Editor;

public class EditorEffectsTests
{
    private static (EditorEffects effects, IDispatcher dispatcher, EditorState state) Setup(
        EditorState? initial = null)
    {
        var stateContainer = Substitute.For<IState<EditorState>>();
        var s = initial ?? new EditorState();
        stateContainer.Value.Returns(s);

        var dispatcher = Substitute.For<IDispatcher>();
        var effects = new EditorEffects(stateContainer);
        return (effects, dispatcher, s);
    }

    [Fact]
    public async Task OpenSubflow_switches_tab_when_already_open()
    {
        var doc = EditorTestData.Document("prepare-import", steps: EditorTestData.Base("k"));
        var initial = EditorReducers.ReduceOpenWorkflowAction(new EditorState(), new OpenWorkflowAction(doc));
        var (effects, dispatcher, _) = Setup(initial);

        await effects.HandleOpenSubflow(new OpenSubflowAction("prepare-import"), dispatcher);

        dispatcher.Received(1).Dispatch(Arg.Is<SwitchTabAction>(a => a.Name == "prepare-import"));
        dispatcher.DidNotReceive().Dispatch(Arg.Any<OpenWorkflowAction>());
    }

    [Fact]
    public async Task OpenSubflow_opens_from_session_cache_when_present()
    {
        var doc = EditorTestData.Document("cached-only", steps: EditorTestData.Base("k"));
        var initial = new EditorState() with
        {
            SubflowCache = new EditorState().SubflowCache.SetItem("cached-only", doc)
        };
        var (effects, dispatcher, _) = Setup(initial);

        await effects.HandleOpenSubflow(new OpenSubflowAction("cached-only"), dispatcher);

        dispatcher.Received(1).Dispatch(Arg.Is<OpenWorkflowAction>(a => ReferenceEquals(a.Document, doc)));
        dispatcher.DidNotReceive().Dispatch(Arg.Any<CreateWorkflowRequestedAction>());
    }

    [Fact]
    public async Task OpenSubflow_creates_empty_draft_when_unknown()
    {
        var (effects, dispatcher, _) = Setup();

        await effects.HandleOpenSubflow(new OpenSubflowAction("brand-new"), dispatcher);

        dispatcher.Received(1).Dispatch(Arg.Is<CreateWorkflowRequestedAction>(a => a.Name == "brand-new"));
    }

    [Fact]
    public Task ImportFileRequested_dispatches_OpenWorkflow_with_filename_as_Name()
    {
        var (effects, dispatcher, _) = Setup();
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
        var (effects, dispatcher, _) = Setup();

        await effects.HandleImportFileRequested(
            new ImportFileRequestedAction("broken.json", "{ not json"), dispatcher);

        dispatcher.Received(1).Dispatch(Arg.Is<ImportFileFailedAction>(a => a.FileName == "broken.json"));
    }

    [Fact]
    public Task CreateWorkflowRequested_dispatches_OpenWorkflow_with_provided_name()
    {
        var (effects, dispatcher, _) = Setup();

        return effects.HandleCreateWorkflowRequested(new CreateWorkflowRequestedAction("new-flow"), dispatcher)
            .ContinueWith(_ => dispatcher.Received(1).Dispatch(Arg.Is<OpenWorkflowAction>(a =>
                a.Document.Name == "new-flow" && a.Document.Steps.Count == 0)));
    }

    [Fact]
    public async Task RenameWorkflowRequested_dispatches_RenameAction_when_target_is_free()
    {
        var (effects, dispatcher, _) = Setup();

        await effects.HandleRenameWorkflowRequested(
            new RenameWorkflowRequestedAction("a", "b", CascadeSubflows: false), dispatcher);

        dispatcher.Received(1).Dispatch(Arg.Is<RenameWorkflowAction>(a =>
            a.OldName == "a" && a.NewName == "b" && a.CascadeSubflows == false));
        dispatcher.DidNotReceive().Dispatch(Arg.Any<RenameWorkflowFailedAction>());
    }

    [Fact]
    public async Task RenameWorkflowRequested_fails_on_conflict()
    {
        var initial = EditorReducers.ReduceOpenWorkflowAction(new EditorState(),
            new OpenWorkflowAction(EditorTestData.Document("import-prices", steps: EditorTestData.Base("k"))));
        var (effects, dispatcher, _) = Setup(initial);

        await effects.HandleRenameWorkflowRequested(
            new RenameWorkflowRequestedAction("import", "import-prices", CascadeSubflows: true), dispatcher);

        dispatcher.Received(1).Dispatch(Arg.Is<RenameWorkflowFailedAction>(a => a.NewName == "import-prices"));
        dispatcher.DidNotReceive().Dispatch(Arg.Any<RenameWorkflowAction>());
    }

    [Fact]
    public async Task RenameWorkflowRequested_fails_on_invalid_name()
    {
        var (effects, dispatcher, _) = Setup();

        await effects.HandleRenameWorkflowRequested(
            new RenameWorkflowRequestedAction("a", "Bad Name!", CascadeSubflows: false), dispatcher);

        dispatcher.Received(1).Dispatch(Arg.Is<RenameWorkflowFailedAction>(a => a.NewName == "Bad Name!"));
    }
}
