using System.Collections.Immutable;
using Fluxor;
using NSubstitute;
using WorkflowEditor.Client.Services.Api;
using WorkflowEditor.Client.Store.Editor;
using WorkflowEditor.Core.Models;
using WorkflowEditor.Core.Models.Steps;
using static WorkflowEditor.Tests.Client.TestKit.EditorTestData;

namespace WorkflowEditor.Tests.Client.Store.Editor;

public class EditorEffectsTests
{
    private readonly IWorkflowApi _api = Substitute.For<IWorkflowApi>();
    private readonly IDispatcher _dispatcher = Substitute.For<IDispatcher>();
    private readonly IState<EditorState> _state = Substitute.For<IState<EditorState>>();

    private EditorEffects CreateEffects(EditorState? initial = null)
    {
        _state.Value.Returns(initial ?? new EditorState());
        return new EditorEffects(_api, _state);
    }

    [Fact]
    public async Task HandleLoadWorkflow_dispatches_success_when_api_returns_document()
    {
        var doc = Document("wf-1", BaseStep("s-1"));
        _api.GetAsync("wf-1", Arg.Any<CancellationToken>())
            .Returns(ApiResult<WorkflowDocument>.Success(doc));
        var effects = CreateEffects();

        await effects.HandleLoadWorkflow(new LoadWorkflowAction("wf-1"), _dispatcher);

        _dispatcher.Received().Dispatch(Arg.Is<LoadWorkflowSuccessAction>(a => a.Document == doc));
    }

    [Fact]
    public async Task HandleLoadWorkflow_dispatches_failed_with_friendly_message_on_NotFound()
    {
        _api.GetAsync("wf-missing", Arg.Any<CancellationToken>())
            .Returns(ApiResult<WorkflowDocument>.NotFound());
        var effects = CreateEffects();

        await effects.HandleLoadWorkflow(new LoadWorkflowAction("wf-missing"), _dispatcher);

        _dispatcher.Received().Dispatch(Arg.Is<LoadWorkflowFailedAction>(a =>
            a.ErrorMessage.Contains("wf-missing")));
    }

    [Fact]
    public async Task HandleLoadWorkflow_dispatches_failed_with_api_error_on_network_failure()
    {
        _api.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ApiResult<WorkflowDocument>.Network("сервер недоступен"));
        var effects = CreateEffects();

        await effects.HandleLoadWorkflow(new LoadWorkflowAction("wf-1"), _dispatcher);

        _dispatcher.Received().Dispatch(Arg.Is<LoadWorkflowFailedAction>(a =>
            a.ErrorMessage.Contains("сервер недоступен")));
    }

    [Fact]
    public async Task HandleSaveWorkflow_dispatches_failure_when_document_is_not_in_state()
    {
        var effects = CreateEffects(new EditorState());

        await effects.HandleSaveWorkflow(new SaveWorkflowAction("wf-1"), _dispatcher);

        _dispatcher.Received().Dispatch(Arg.Is<SaveWorkflowFailedAction>(a => a.WorkflowId == "wf-1"));
        await _api.DidNotReceive().SaveAsync(Arg.Any<WorkflowDocument>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleSaveWorkflow_dispatches_success_when_api_succeeds()
    {
        var doc = Document("wf-1");
        var initial = StateWith(doc);
        _api.SaveAsync(doc, Arg.Any<CancellationToken>())
            .Returns(ApiResult<Unit>.Success(Unit.Value));
        var effects = CreateEffects(initial);

        await effects.HandleSaveWorkflow(new SaveWorkflowAction("wf-1"), _dispatcher);

        _dispatcher.Received().Dispatch(Arg.Is<SaveWorkflowSuccessAction>(a => a.WorkflowId == "wf-1"));
    }

    [Fact]
    public async Task HandleSaveWorkflow_dispatches_failure_when_api_fails()
    {
        var doc = Document("wf-1");
        var initial = StateWith(doc);
        _api.SaveAsync(doc, Arg.Any<CancellationToken>())
            .Returns(ApiResult<Unit>.ServerError("boom"));
        var effects = CreateEffects(initial);

        await effects.HandleSaveWorkflow(new SaveWorkflowAction("wf-1"), _dispatcher);

        _dispatcher.Received().Dispatch(Arg.Is<SaveWorkflowFailedAction>(a =>
            a.WorkflowId == "wf-1" && a.ErrorMessage == "boom"));
    }

    [Fact]
    public async Task HandleCreateWorkflowRequested_dispatches_OpenWorkflow_with_new_document()
    {
        var effects = CreateEffects(new EditorState());

        await effects.HandleCreateWorkflowRequested(new CreateWorkflowRequestedAction(), _dispatcher);

        _dispatcher.Received().Dispatch(Arg.Is<OpenWorkflowAction>(a =>
            !string.IsNullOrEmpty(a.Document.WorkflowId)
            && a.Document.Steps.Count == 0
            && a.Document.Links.Count == 0));
    }

    [Fact]
    public async Task HandleCreateWorkflowRequested_numbers_documents_starting_from_existing_count()
    {
        var existing = Document("wf-existing");
        var initial = new EditorState() with
        {
            OpenDocuments = ImmutableDictionary<string, WorkflowDocument>.Empty.SetItem("wf-existing", existing)
        };
        var effects = CreateEffects(initial);

        await effects.HandleCreateWorkflowRequested(new CreateWorkflowRequestedAction(), _dispatcher);

        _dispatcher.Received().Dispatch(Arg.Is<OpenWorkflowAction>(a => a.Document.Name == "Процесс 2"));
    }
}
