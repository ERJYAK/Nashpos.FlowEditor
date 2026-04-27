# Workflow Editor

Веб-редактор бизнес-процессов: пользователь рисует «процесс» из узлов и связей, сохраняет на сервер и снова открывает по ID. Узел может быть `base` (заглушка) или `subflow` (ссылка на другой процесс — раскрывается во вкладке). Документ хранится как иммутабельный snapshot (proto в транспорте, JSON в БД).

Подходит как редактор для конструктора пайплайнов / сценариев / визуальных автоматизаций, где целевой потребитель документа отдельный (исполнитель/runtime — за пределами репо).

---

## Что умеет (продуктово)

- Создание/открытие/сохранение/удаление процессов (CRUD), список процессов.
- Несколько процессов одновременно — вкладки, переключение, закрытие.
- Узлы: `base`, `subflow` (двойной клик открывает связанный процесс новой вкладкой).
- Связи между портами узлов (`Left`/`Right`/`Top`/`Bottom`).
- Выделение, drag-and-drop, multi-select rubber-band, copy/paste, delete.
- Per-document Undo/Redo (Ctrl+Z / Ctrl+Y).
- Dirty-флаг и подсветка вкладки с несохранёнными правками.
- Импорт/экспорт документа в JSON через отдельные use-case'ы.

Известные продуктовые ограничения:
- Хранилище — **EF Core InMemory**, данные не переживают рестарт сервера. Постоянное хранилище — задача этапа 8 платформы.
- Авторизации/мультитенантности нет.
- UI на русском (литералы захардкожены, локализация не вводилась).

---

## Стек

- .NET 10, C# (latest), `Nullable=enable`, `ImplicitUsings=enable`.
- **Frontend:** Blazor WebAssembly + [Fluxor](https://github.com/mrpmorris/Fluxor) (Redux/Flux) + [MudBlazor](https://mudblazor.com/) + [Z.Blazor.Diagrams](https://github.com/Blazor-Diagrams/Blazor.Diagrams).
- **Backend:** ASP.NET Core Kestrel, Grpc.AspNetCore + Grpc.AspNetCore.Web.
- **Контракт:** gRPC (`workflow.proto`) c типизированными сообщениями `Step`/`Link`/`Position` и `oneof StepKind`.
- **Persistence:** EF Core (Microsoft.EntityFrameworkCore.InMemory), payload — JSON-сериализованный `WorkflowDocument`, версионируется через concurrency token `Version`.
- **Validation:** FluentValidation в Application-слое + gRPC `ExceptionInterceptor` мапит `Error → RpcException`.
- **Тесты:** xUnit + FluentAssertions + NSubstitute + Verify (snapshot).
- **CI:** GitHub Actions (`.github/workflows/ci.yml`) — `dotnet restore && build && test` на push/PR в `master`.
- **Pkg management:** Central Package Management (`Directory.Packages.props`); только nuget.org (`NuGet.config` чистит унаследованные приватные фиды).

---

## Запуск локально

```bash
dotnet restore WorkflowEditor.sln
dotnet build   WorkflowEditor.sln
dotnet test    WorkflowEditor.sln
```

API + клиент (двумя терминалами):

```bash
dotnet run --project WorkflowEditor.Api    # https://localhost:7242  http://localhost:5116
dotnet run --project WorkflowEditor.Client # Blazor WASM dev-server
```

URL gRPC у клиента читается из `WorkflowEditor.Client/wwwroot/appsettings.json` (`Api:GrpcUrl`). По умолчанию `https://localhost:7242` — совпадает с Api.

CORS-список для прода — `Cors:AllowedOrigins` в `WorkflowEditor.Api/appsettings.json` (пустой массив = `AllowAnyOrigin`).

---

## Архитектура

Clean Architecture (light) + vertical slices внутри Application. Зависимости направлены строго внутрь (Core ← Application ← Infrastructure / Api / Client; Contracts — поперечный shared layer для proto).

```
WorkflowEditor.Core            доменные модели, без зависимостей
WorkflowEditor.Application     use-case'ы, валидаторы, Result<T>/Error
WorkflowEditor.Infrastructure  EF Core, AppDbContext, WorkflowRepository
WorkflowEditor.Contracts       workflow.proto + WorkflowProtoMapper (proto↔domain)
WorkflowEditor.Api             Kestrel, gRPC service, interceptors, DI composition
WorkflowEditor.Client          Blazor WASM, Fluxor store, диаграмма
WorkflowEditor.Tests.Server    xUnit: Application + Infrastructure + Contracts
WorkflowEditor.Tests.Client    xUnit: Core, Serialization (Verify), Store/Editor (reducers + effects)
```

Поток типичной операции «Save» (E2E):

```
UI (Editor.razor)
  → Dispatch(SaveWorkflowAction)
  → EditorEffects.HandleSaveWorkflow
  → IWorkflowApi.SaveAsync (GrpcWorkflowApi, маппинг domain → proto)
  → gRPC-Web → ASP.NET → ExceptionInterceptor
  → WorkflowStorageService.SaveWorkflow (тонкий, маппит proto → domain)
  → SaveWorkflowCommandHandler (FluentValidation + Upsert)
  → WorkflowRepository.UpsertAsync (EF Core, JSON в PayloadJson, Version++)
  → Result<WorkflowDocument> ← (или Error → RpcException через GrpcResultMapper)
  → ApiResult<Unit> ← (на клиенте)
  → Dispatch(SaveWorkflowSuccess|SaveWorkflowFailedAction)
  → MainLayout подписан, показывает Snackbar
```

### Карта папок

```
WorkflowEditor.Core/
  Models/                WorkflowDocument, WorkflowStep (abstract), WorkflowLink, CanvasPosition
  Models/Steps/          BaseStep, SubflowStep   (← добавляем сюда новые типы)
  Serialization/         JsonConfiguration, WorkflowStep/LinkDictionaryConverter

WorkflowEditor.Application/
  Abstractions/          IWorkflowRepository, WorkflowSummary
  Common/                Result<T>, Error (NotFound/Validation/Conflict/Unexpected)
  Workflows/{Get|Save|List|Delete|Import|Export}/
                         {Query|Command} + Handler + (Validator)
  DependencyInjection.cs AddApplication() — handlers, validators, JSON migrator

WorkflowEditor.Infrastructure/
  Persistence/           AppDbContext, WorkflowEntity, WorkflowRepository
  DependencyInjection.cs AddInfrastructure(IConfiguration)

WorkflowEditor.Contracts/
  Protos/workflow.proto  GrpcServices="Both" — генерит и server-base, и client
  Mapping/WorkflowProtoMapper.cs

WorkflowEditor.Api/
  Grpc/GrpcResultMapper.cs   Error → RpcException
  Interceptors/ExceptionInterceptor.cs
  Services/WorkflowStorageService.cs    (тонкий gRPC façade над handlers)
  Program.cs, appsettings.json

WorkflowEditor.Client/
  Pages/Editor.razor             root UI, app-bar, табы, контекстное меню, шорткаты
  Components/CanvasTab.razor     адаптер Z.Blazor.Diagrams ↔ EditorState
  Components/Nodes/              MudBlazor-виджеты для BaseStep / SubflowStep
  Diagram/Nodes/                 NodeModel'и для Z.Blazor.Diagrams
  Services/Api/                  IWorkflowApi, GrpcWorkflowApi, ApiResult<T>
  Store/Editor/                  EditorState, EditorActions, EditorReducers, EditorEffects
```

### Ключевые инварианты state (Frontend)

- `EditorState`, `WorkflowDocument`, `WorkflowStep`, `WorkflowLink` — record'ы, все поля `init`. Изменения только через `with`.
- `Steps`/`Links` хранятся как `ImmutableDictionary<string, T>` (O(1) lookup, без копий списка на каждое движение мыши). В JSON выводятся как массив через кастомные конвертеры (с детерминированной сортировкой по `Id` — иначе snapshot-тест мигает).
- Редьюсеры — чистые: никаких `Guid.NewGuid()` / `Console.WriteLine` / `try-catch` внутри. Создание GUID для нового документа сидит в `EditorEffects.HandleCreateWorkflowRequested`.
- Полиморфизм шагов — через абстрактные `WithName/WithPosition/CloneWithId` на `WorkflowStep`. Никаких `switch`-ей по типу шага в редьюсерах.

---

## Контракт данных

### Транспорт (gRPC)

`WorkflowEditor.Contracts/Protos/workflow.proto` — единственный источник истины для проводов:

```proto
service WorkflowStorage {
  rpc GetWorkflow    (GetWorkflowRequest)    returns (GetWorkflowResponse);
  rpc SaveWorkflow   (SaveWorkflowRequest)   returns (SaveWorkflowResponse);
  rpc ListWorkflows  (ListWorkflowsRequest)  returns (ListWorkflowsResponse);
  rpc DeleteWorkflow (DeleteWorkflowRequest) returns (DeleteWorkflowResponse);
}

message Step {
  string id = 1; string name = 2; Position position = 3;
  oneof kind { BaseStepData base = 10; SubflowStepData subflow = 11; }
}
```

Маппинг proto↔domain — `WorkflowProtoMapper`. Миссинг `oneof kind` бросает понятную ошибку (forward-incompatible payload, тест `WorkflowProtoMapperTests`).

### Storage / экспорт (JSON)

`WorkflowDocument` сериализуется через `JsonConfiguration.GetOptions()`: camelCase, полиморфизм через дискриминатор `"type"` (`UnknownDerivedTypeHandling = FailSerialization` — на неизвестном типе бросаем).

Снимок формата зафиксирован snapshot-тестом:

```
WorkflowEditor.Tests.Client/Serialization/
  WorkflowDocumentJsonSnapshotTests.Document_with_mixed_step_types_and_link_serializes_to_known_shape.verified.json
```

При любом изменении модели `WorkflowDocument` / `WorkflowStep` / `WorkflowLink` / `JsonConfiguration` snapshot падает. Diff читать глазами, осознанно принимать `.received → .verified`. **Любое breaking-изменение JSON-формата требует миграции** (см. ниже).

---

## Точки расширения

### 1. Новый тип шага

1. Наследник `WorkflowStep` в `WorkflowEditor.Core/Models/Steps/` — реализовать `WithName/WithPosition/CloneWithId`.
2. `[JsonDerivedType(typeof(MyStep), "myKind")]` на `WorkflowStep`.
3. В `workflow.proto` добавить ветку `oneof kind { ... MyStepData my = 12; }`.
4. В `WorkflowProtoMapper.ToProto/FromProto` обработать новую ветку (компилятор подскажет: switch не покрывает).
5. В `WorkflowEditor.Client/Diagram/Nodes/` — `NodeModel` для движка диаграммы.
6. В `WorkflowEditor.Client/Components/Nodes/` — Razor-виджет.
7. Зарегистрировать в `CanvasTab.razor`: `Diagram.RegisterComponent<MyNodeModel, MyNodeWidget>()`.
8. Добавить пункт меню в `Editor.razor` (`AddStepToActive("myKind")`).

Тесты: `WorkflowStepPolymorphismTests`, snapshot, `WorkflowProtoMapperTests`.

### 2. Миграция старого JSON

Если меняется breaking-формат — пишется реализация `IWorkflowDocumentJsonMigrator` (`WorkflowEditor.Application/Workflows/Import/`). Сейчас стоит `IdentityWorkflowDocumentJsonMigrator` (passthrough). Импорт всегда проходит через мигратор, ошибки маппятся в `Error.Validation`.

### 3. Новый use-case

Папка `WorkflowEditor.Application/Workflows/<Name>/`:
- `<Name>Command.cs` или `<Name>Query.cs` (record + интерфейс handler'а),
- `<Name>Handler.cs` — возвращает `Result<T>`, тянет `IWorkflowRepository` + при необходимости `IValidator<T>`,
- `<Name>Validator.cs` (если нужен FluentValidation),
- регистрация в `WorkflowEditor.Application.DependencyInjection`,
- если нужен RPC — RPC в `workflow.proto`, метод в `WorkflowStorageService`, маппинг через `WorkflowProtoMapper`.

---

## Тесты и CI

- `dotnet test` гоняет 2 проекта: `Tests.Client` (66 тестов: Core, Serialization, Store reducers + effects) и `Tests.Server` (24 теста: Application handlers через NSubstitute, Infrastructure через EF InMemory, Contracts mapper). Покрытие — характеризационное: фиксируем поведение, чтобы не проседало при изменениях.
- Snapshot-тест `WorkflowDocumentJsonSnapshotTests` — единственный «контракт хранения». Новые `.received.json` рядом с `.verified.json` — diff и осознанный апрув.
- CI — `.github/workflows/ci.yml`: `restore → build (Release) → test`. Артефакт — `test-results.trx`.

---

## Конфигурация

| Где                                      | Ключ                          | По умолчанию                  | Назначение                                  |
|------------------------------------------|-------------------------------|-------------------------------|---------------------------------------------|
| `WorkflowEditor.Api/appsettings.json`    | `Cors:AllowedOrigins`         | `[]` (= `AllowAnyOrigin`)     | Origins для CORS                            |
| `WorkflowEditor.Api/appsettings.json`    | `Database:InMemoryName`       | `"workflows"`                 | Имя in-memory БД EF Core                    |
| `WorkflowEditor.Client/wwwroot/appsettings.json` | `Api:GrpcUrl`         | `https://localhost:7242`      | URL gRPC-сервера для клиента                |
| `WorkflowEditor.Api/Properties/launchSettings.json` | `applicationUrl`   | `https://localhost:7242;http://localhost:5116` | Порты Kestrel                |

Health-check: `GET /health` → `{ status: "healthy" }`.

---

## Известные ограничения / TODO

Подробный аудит и план в [`WorkflowEditor.Client/AGENT.md`](WorkflowEditor.Client/AGENT.md). Из этого плана **остался только этап 8 (платформа):**

- Postgres вместо InMemory + EF миграции.
- CSS вынести в `*.razor.css` (CSS isolation), убрать `!important` и инлайн `<style>` в `MainLayout` / `CanvasTab`.
- Заменить остатки `Console.WriteLine` на `ILogger<T>`.
- `/ready` endpoint, Docker compose.
