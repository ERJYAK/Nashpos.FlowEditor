# Workflow Editor

Веб-редактор бизнес-процессов: пользователь рисует «процесс» из узлов и связей, сохраняет на сервер либо как `.json`-файл, и снова открывает по имени. Базовый шаг (`step`) — ссылка на зарегистрированный обработчик. Subflow-шаг (`subflow`) — ссылка на другой процесс по имени; раскрывается двойным кликом во вкладку. Документ хранится в JSON-формате, идентичном тому, который потребляет downstream-исполнитель.

---

## Что умеет (продуктово)

- Визуальное создание процессов: drag узлов на холсте, направленные связи между ними (стрелка указывает порядок выполнения).
- Multi-tab: одновременно несколько открытых процессов, dirty-флаг (`●`) на вкладке.
- **Импорт JSON-файла:** drag-and-drop файла на окно или кнопка «Загрузить файл». Имя процесса = имя файла.
- **Экспорт:** «Сохранить файл» скачивает текущий процесс как `<name>.json` в исходном бизнес-формате.
- **Сохранение на сервер:** «Сохранить на сервер» отправляет процесс через gRPC.
- **Subflow-узел** показывает имя подпроцесса + пронумерованный список вложенных шагов (lazy-fetch с сервера, кэш в Fluxor). Двойной клик — открывает подпроцесс в новой вкладке (если на сервере нет — открывается пустой черновик с этим именем). На рекурсию (subflow ссылается на уже открытый процесс) — badge «↻ recursive», без раскрытия.
- Per-document **Undo/Redo** (Ctrl+Z / Ctrl+Y).
- Auto-layout вертикальной цепочкой при импорте (бизнес-формат не хранит координаты).
- Валидация при сохранении: только линейная цепочка (≤1 in, ≤1 out, один head, без циклов и оторванных узлов). Ошибки — Snackbar.

Текущие ограничения:
- Хранилище — **EF Core InMemory**. Данные не переживают рестарт сервера.
- Авторизации/мультитенантности нет.
- Редактор `context` — пока не реализован (загруженный context сохраняется без потерь, но изменить его в UI нельзя). Editing-инструменты для `description`/`StepKind` базовых шагов — TODO.

---

## Стек

- .NET 10, C# (latest), `Nullable=enable`, `ImplicitUsings=enable`.
- **Frontend:** Blazor WebAssembly + [Fluxor](https://github.com/mrpmorris/Fluxor) (Redux/Flux) + [MudBlazor](https://mudblazor.com/) + [Z.Blazor.Diagrams](https://github.com/Blazor-Diagrams/Blazor.Diagrams).
- **Backend:** ASP.NET Core Kestrel, Grpc.AspNetCore + Grpc.AspNetCore.Web.
- **Контракт:** gRPC (`workflow.proto`) с типизированными `Step`/`StepContext`/`oneof StepKind { BaseStepData base; SubflowStepData subflow }`. `StepContext.Objects` использует `google.protobuf.Value` для произвольных вложенных JSON-структур.
- **Persistence:** EF Core (Microsoft.EntityFrameworkCore.InMemory), payload — JSON-сериализованный `WorkflowDocument`, ключ строки = `Name`, версионирование через concurrency-token `Version`.
- **Validation:** FluentValidation в Application + gRPC `ExceptionInterceptor` мапит `Error → RpcException`.
- **Тесты:** xUnit + FluentAssertions + NSubstitute + Verify (snapshot).
- **CI:** GitHub Actions (`.github/workflows/ci.yml`).
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

URL gRPC у клиента читается из `WorkflowEditor.Client/wwwroot/appsettings.json` (`Api:GrpcUrl`).
CORS-список для прода — `Cors:AllowedOrigins` в `WorkflowEditor.Api/appsettings.json` (пустой массив = `AllowAnyOrigin`).

---

## Формат документа

Целевой формат бизнес-документа (одна `.json`-файл = один процесс, имя файла без `.json` = ключ хранения):

```jsonc
{
  "description": "Import flow",
  "steps": [
    { "subflow": "prepare-import", "description": "Prepare import subflow" },

    { "subflow": "iterate-tenants", "description": "Iterate through the tenant list",
      "iterate": true,
      "context": {
        "strings": { "collection.name": "tenants" }
      }
    },

    { "step": "apply-import", "description": "Transfer imported data" }
  ]
}
```

Дискриминатор шага = **наличие ключа** (`step` xor `subflow`), не значение общего поля. `WorkflowStepJsonConverter` парсит это вручную (стандартный `[JsonPolymorphic]` так не умеет). Поле `name` в JSON НЕ хранится — это `[JsonIgnore]` + ключ хранения, задаваемый именем файла или модальным диалогом «Создать процесс».

`context` — типизированный мешок: `strings: Dict<string,string>`, `integers: Dict<string,long>`, `objects: Dict<string,JsonElement>` (JsonElement — для произвольной вложенности типа `where: { tenant_id: "abc" }`).

Реальные production-файлы лежат в `WorkflowEditor.Tests.Server/TestFlows/*.json` (5 шт.) и проверяются roundtrip-тестом `RealFileRoundtripTests` в `WorkflowEditor.Tests.Client/Serialization/`.

---

## Архитектура

Clean Architecture (light) + vertical slices внутри Application. Зависимости направлены внутрь (Core ← Application ← Infrastructure / Api / Client; Contracts — поперечный shared layer для proto + mapper).

```
WorkflowEditor.Core            доменные модели + JsonConfiguration + WorkflowStepJsonConverter
WorkflowEditor.Application     use-case'ы (Get/Save/List/Delete/Import/Export), Result<T>/Error
WorkflowEditor.Infrastructure  EF Core, AppDbContext, WorkflowRepository (PK = Name)
WorkflowEditor.Contracts       workflow.proto + WorkflowProtoMapper (proto↔domain)
WorkflowEditor.Api             Kestrel, gRPC service, ExceptionInterceptor, DI composition
WorkflowEditor.Client          Blazor WASM, Fluxor store, диаграмма, JS-interop
WorkflowEditor.Tests.Server    Application + Infrastructure + Contracts (NSubstitute, EF InMemory)
WorkflowEditor.Tests.Client    Core + Serialization + Store (reducers/effects) + Topology + Layout
```

### Поток сохранения (E2E)

```
UI (Editor.razor «Сохранить на сервер»)
  → Dispatch(SaveWorkflowAction(name))
  → EditorEffects.HandleSaveWorkflow
      ├─ StepOrderResolver: проверить, что граф линейный → отсортировать
      └─ IWorkflowApi.SaveAsync (GrpcWorkflowApi → WorkflowProtoMapper.ToProto → gRPC-Web)
  → WorkflowStorageService.SaveWorkflow → SaveWorkflowCommandHandler
       (SaveWorkflowValidator: name regex, XOR StepKind/SubflowName)
  → WorkflowRepository.UpsertAsync (EF Core, JSON в PayloadJson, Version++)
  → Result<WorkflowDocument> ← (или Error → RpcException через GrpcResultMapper)
  → ApiResult<Unit> ← на клиенте → SaveWorkflowSuccess|FailedAction
  → MainLayout: Snackbar
```

### Карта папок

```
WorkflowEditor.Core/
  Models/                WorkflowDocument, WorkflowStep, CanvasPosition, StepContext
  Models/Steps/          BaseStep, SubflowStep   (← добавляем сюда новые типы)
  Serialization/         JsonConfiguration, WorkflowStepJsonConverter

WorkflowEditor.Application/
  Abstractions/          IWorkflowRepository, WorkflowSummary
  Common/                Result<T>, Error (NotFound/Validation/Conflict/Unexpected)
  Workflows/{Get|Save|List|Delete|Import|Export}/
                         {Query|Command} + Handler + (Validator/Migrator)

WorkflowEditor.Infrastructure/
  Persistence/           AppDbContext (HasKey(Name)), WorkflowEntity, WorkflowRepository

WorkflowEditor.Contracts/
  Protos/workflow.proto  GrpcServices="Both" (server-base + client)
  Mapping/WorkflowProtoMapper.cs   домен ↔ proto, StepContext ↔ google.protobuf.Value

WorkflowEditor.Api/
  Grpc/GrpcResultMapper.cs           Error → RpcException
  Interceptors/ExceptionInterceptor.cs
  Services/WorkflowStorageService.cs (тонкий gRPC façade над handlers)

WorkflowEditor.Client/
  Pages/Editor.razor                 root UI, app-bar, табы, file menu, drag-drop init
  Components/CanvasTab.razor         адаптер Z.Blazor.Diagrams ↔ EditorState (направленные связи)
  Components/Nodes/                  MudBlazor-виджеты для BaseStep / SubflowStep
  Components/Dialogs/NameDialog.razor  модалка «Имя процесса»/«Имя подпроцесса»
  Diagram/Nodes/                     NodeModel'и для Z.Blazor.Diagrams
  Services/Api/                      IWorkflowApi, GrpcWorkflowApi, ApiResult<T>
  Services/Files/                    IFileDownloader (JS-interop через wwwroot/js/file-download.js)
  Services/Layout/LinearAutoLayout   вертикальная цепочка top-down при импорте
  Services/Topology/StepOrderResolver  валидация линейности перед save
  Store/Editor/                      EditorState, EditorActions, EditorReducers, EditorEffects
  wwwroot/js/file-download.js        Blob → download link
  wwwroot/js/file-drop.js            HTML5 drag-drop через DotNetObjectReference
```

### Ключевые инварианты state (Frontend)

- `EditorState` ключи = `Name` (string). `WorkflowDocument`, `WorkflowStep` — record'ы, `init`-only. Изменения через `with`.
- `OpenDocuments[name]: EditorDocument = { Document: WorkflowDocument, Links: Dict<id,EditorLink>, NodePositions: Dict<stepId, CanvasPosition> }`. **Links и NodePositions — UI-only**, на сервер не уезжают (сервер видит только упорядоченный массив `Document.Steps`).
- `WorkflowDocument.Steps` — `ImmutableList<WorkflowStep>`, порядок = бизнес-семантика. ID шага синтетический (`Guid`), `[JsonIgnore]`, существует только в браузерной сессии.
- `SubflowCache: Dict<name, WorkflowDocument>` + `LoadingSubflows: HashSet<name>` — кэш для отображения шагов внутри SubflowNodeWidget. Инвалидируется на любой `SaveWorkflowSuccess`.
- Редьюсеры — чистые: никаких `Guid.NewGuid()` / I/O. Создание GUID для нового документа — в `EditorEffects.HandleCreateWorkflowRequested`.
- Полиморфизм шагов — через абстрактные `WithDescription` и `CloneAsNew`, плюс `WithStepKind` / `WithSubflowName` в наследниках.

---

## Точки расширения

### 1. Новый тип шага

1. Наследник `WorkflowStep` в `WorkflowEditor.Core/Models/Steps/`. Реализовать `WithDescription`, `CloneAsNew` и type-specific метод (см. `BaseStep.WithStepKind`).
2. `WorkflowStepJsonConverter`: добавить ветку `step`/`subflow`/`<новый_дискриминатор>` в `Read`/`Write` (компилятор не подскажет — есть тест на missing-discriminator).
3. `workflow.proto` → новая ветка `oneof kind { ... NewKindData new = 12; }`.
4. `WorkflowProtoMapper.ToProto/FromProto` — обработать новую ветку (компилятор подскажет про `KindCase`).
5. Frontend: `Diagram/Nodes/<New>NodeModel.cs` + `Components/Nodes/<New>NodeWidget.razor`.
6. Зарегистрировать в `CanvasTab.razor`: `Diagram.RegisterComponent<NewNodeModel, NewNodeWidget>()`.
7. Кнопка/пункт меню «Добавить шаг → новый» в `Editor.razor`.

Тесты: `WorkflowStepPolymorphismTests`, `WorkflowDocumentJsonDeserializationTests`, `WorkflowProtoMapperTests`. Roundtrip с реальным файлом нового вида — добавить файл в `WorkflowEditor.Tests.Server/TestFlows/`.

### 2. Миграция старого JSON

Если меняется breaking-формат — реализация `IWorkflowDocumentJsonMigrator` (`WorkflowEditor.Application/Workflows/Import/`). Сейчас стоит `IdentityWorkflowDocumentJsonMigrator` (passthrough). Импорт всегда проходит через мигратор; ошибки маппятся в `Error.Validation`.

### 3. Новый use-case

Папка `WorkflowEditor.Application/Workflows/<Name>/`:
- `<Name>Command.cs` или `<Name>Query.cs` (record + интерфейс handler'а),
- `<Name>Handler.cs` — возвращает `Result<T>`, тянет `IWorkflowRepository` + при необходимости `IValidator<T>`,
- регистрация в `WorkflowEditor.Application.DependencyInjection`,
- если нужен RPC — добавить в `workflow.proto`, методу в `WorkflowStorageService` маппинг через `WorkflowProtoMapper`.

---

## Тесты и CI

- `dotnet test` — два проекта: **Tests.Server (23)** и **Tests.Client (50)**, всего 73 теста.
- **Главный «золотой» тест:** `RealFileRoundtripTests` в `Tests.Client/Serialization/` гоняет все 5 файлов из `Tests.Server/TestFlows/` через десериализацию + сериализацию + `JsonElement.DeepEquals` против оригинала.
- **Snapshot:** `WorkflowDocumentJsonSnapshotTests` фиксирует канонический вид документа (Verify). При изменении модели/конвертера — `.received.json` появляется рядом с `.verified.json`, нужно осознанно принять.
- **CI** — `.github/workflows/ci.yml`: `restore → build (Release) → test`. Артефакт — `test-results.trx`.

---

## Конфигурация

| Где                                              | Ключ                       | По умолчанию                                   | Назначение                                           |
|--------------------------------------------------|----------------------------|------------------------------------------------|------------------------------------------------------|
| `WorkflowEditor.Api/appsettings.json`            | `Cors:AllowedOrigins`      | `[]` (= `AllowAnyOrigin`)                      | Origins для CORS                                     |
| `WorkflowEditor.Api/appsettings.json`            | `Database:InMemoryName`    | `"workflows"`                                  | Имя in-memory БД EF Core                             |
| `WorkflowEditor.Client/wwwroot/appsettings.json` | `Api:GrpcUrl`              | `https://localhost:7242`                       | URL gRPC-сервера для клиента                         |
| `WorkflowEditor.Api/Properties/launchSettings.json` | `applicationUrl`        | `https://localhost:7242;http://localhost:5116` | Порты Kestrel                                        |

Health-check: `GET /health` → `{ status: "healthy" }`.

---

## Известные ограничения / TODO

- Postgres вместо InMemory + EF миграции.
- Полноценный редактор `context` в UI (сейчас context загружается/сохраняется без потерь, но изменить его в редакторе нельзя).
- Edit-режим для `description` / `StepKind` базовых шагов: сейчас они задаются только при создании (через диалог имени) или импортом из файла.
- CSS вынести в `*.razor.css` (CSS isolation), убрать `!important` и инлайн `<style>` в `MainLayout` / `CanvasTab`.
- Заменить остатки `Console.WriteLine` на `ILogger<T>`.
- `/ready` endpoint, Docker compose.
