# Workflow Editor

Веб-редактор бизнес-процессов: пользователь рисует «процесс» из узлов и связей и сохраняет его как `.json`-файл в бизнес-формате, который потребляет downstream-исполнитель. Базовый шаг (`step`) — ссылка на зарегистрированный обработчик; шаг `execute-js-script` — встроенный JS; subflow-шаг (`subflow`) — ссылка на другой процесс по имени, раскрывается двойным кликом во вкладку.

Приложение полностью клиентское (Blazor WebAssembly). Бэкенда нет: данные живут в текущей сессии браузера, обмен с внешним миром — через импорт/экспорт `.json`-файлов.

---

## Что умеет (продуктово)

- Визуальное создание процессов: drag узлов на холсте, направленные связи между ними (стрелка указывает порядок выполнения), множественное выделение рамкой, copy/paste.
- Типы узлов: базовый шаг, `execute-js-script` (Monaco-редактор скрипта), subflow, плюс виртуальные STOP-узлы для веток `BREAK_WORKFLOW` / `SILENT_BREAK_WORKFLOW`.
- Условные переходы: `onSuccess` / `onFail` / `whenCode` и брейкпоинты — диалог «Условия и брейкпоинт».
- Multi-tab: одновременно несколько открытых процессов, dirty-флаг (`●`) на вкладке, drag-reorder вкладок, восстановление закрытой вкладки.
- **Импорт JSON-файла:** drag-and-drop файла на окно или кнопка «Загрузить файл». Имя процесса = имя файла.
- **Экспорт:** «Сохранить файл» скачивает текущий процесс как `<name>.json` в бизнес-формате (с валидацией графа).
- **Subflow-узел** показывает имя подпроцесса + пронумерованный список вложенных шагов (из сессионного кэша — если подпроцесс был открыт/импортирован). Двойной клик открывает подпроцесс: из кэша, либо как новый пустой черновик с этим именем.
- Per-document **Undo/Redo** (Ctrl+Z / Ctrl+Y).
- Тёмная/светлая тема (запоминается в `localStorage`).
- Auto-layout вертикальной цепочкой при импорте (бизнес-формат не хранит координаты).
- Валидация при экспорте: только линейная цепочка (≤1 in, ≤1 out, один head, без циклов и оторванных узлов). Ошибки — Snackbar + красная обводка невалидных узлов.

Текущие ограничения:
- **Никакой персистентности между сессиями:** открытые/закрытые документы и subflow-кэш теряются при F5. Долговременное хранение — только экспорт в файл.
- Авторизации/мультитенантности нет.

---

## Стек

- .NET 10, C# (latest), `Nullable=enable`, `ImplicitUsings=enable`.
- **Frontend:** Blazor WebAssembly + [Fluxor](https://github.com/mrpmorris/Fluxor) (Redux/Flux) + [MudBlazor](https://mudblazor.com/) + [Z.Blazor.Diagrams](https://github.com/Blazor-Diagrams/Blazor.Diagrams) + Monaco (JS-редактор через JS-interop).
- **Сериализация:** `System.Text.Json` с кастомным `WorkflowStepJsonConverter` и relaxed-энкодером (кириллица/символы пишутся как есть, не `\uXXXX`).
- **Тесты:** xUnit + FluentAssertions + NSubstitute + Verify (snapshot).
- **CI:** GitHub Actions (`.github/workflows/ci.yml`).
- **Pkg management:** Central Package Management (`Directory.Packages.props`); только nuget.org (`NuGet.config` чистит унаследованные приватные фиды).

---

## Запуск локально

```bash
dotnet restore WorkflowEditor.sln
dotnet build   WorkflowEditor.sln
dotnet test    WorkflowEditor.sln

dotnet run --project WorkflowEditor.Client   # Blazor WASM dev-server
```

Серверная часть не требуется — клиент самодостаточен.

---

## Формат документа

Целевой формат бизнес-документа (один `.json`-файл = один процесс, имя файла без `.json` = имя процесса):

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

Дискриминатор шага = **наличие ключа** (`step` xor `subflow`), не значение общего поля. `WorkflowStepJsonConverter` парсит это вручную (стандартный `[JsonPolymorphic]` так не умеет). Поле `name` в JSON НЕ хранится — это `[JsonIgnore]` + имя, задаваемое именем файла или модальным диалогом «Создать процесс».

`context` — типизированный мешок: `strings: Dict<string,string>`, `integers: Dict<string,long>`, `objects: Dict<string,JsonElement>` (JsonElement — для произвольной вложенности типа `where: { tenant_id: "abc" }`). Скрипт шага `execute-js-script` лежит в `context.strings["script"]`.

---

## Архитектура

Два проекта кода + тесты. Зависимости направлены внутрь (Core ← Client).

```
WorkflowEditor.Core          доменные модели + JsonConfiguration + WorkflowStepJsonConverter
WorkflowEditor.Client        Blazor WASM, Fluxor store, диаграмма, JS-interop
WorkflowEditor.Tests.Client  Core + Serialization + Store (reducers/effects) + Topology + Layout
```

### Поток сохранения в файл

```
UI (Editor.razor «Сохранить файл»)
  → WorkflowGraphValidator.ValidateForExport (граф линейный? иначе — пометить невалидные узлы + Snackbar)
  → JsonSerializer.Serialize(Document, JsonConfiguration.GetOptions())
  → IFileDownloader.DownloadAsync (JS-interop: Blob → download link)
```

### Карта папок

```
WorkflowEditor.Core/
  Models/                WorkflowDocument, WorkflowStep, CanvasPosition, StepContext, Branch, BreakpointConfig
  Models/Steps/          BaseStep, SubflowStep   (← добавляем сюда новые типы)
  Serialization/         JsonConfiguration, WorkflowStepJsonConverter

WorkflowEditor.Client/
  Pages/Editor.razor                 root UI, app-bar, табы, file menu, drag-drop init
  Components/CanvasTab.razor         адаптер Z.Blazor.Diagrams ↔ EditorState (направленные связи, темы)
  Components/MainLayout.razor        тема (dark/light), Snackbar-подписки
  Components/Nodes/                  MudBlazor-виджеты: Base / JsScript / Subflow / Stop
  Components/Dialogs/                NameDialog, EditStep, EditStepBranches, EditJsScript, RestoreWorkflow, Confirm
  Components/MonacoJsEditor.razor    обёртка над Monaco
  Diagram/Nodes/                     NodeModel'и для Z.Blazor.Diagrams
  Services/Files/                    IFileDownloader (JS-interop через wwwroot/js/file-download.js)
  Services/Layout/LinearAutoLayout   вертикальная цепочка top-down при импорте
  Services/Topology/                 BranchLinkBuilder (links/STOP из веток), WorkflowGraphValidator (линейность)
  Store/Editor/                      EditorState, EditorActions, EditorReducers, EditorEffects
  wwwroot/js/                        file-download.js, file-drop.js, monaco-interop.js, tab-reorder.js
```

### Ключевые инварианты state (Frontend)

- `EditorState` ключи = `Name` (string). `WorkflowDocument`, `WorkflowStep` — record'ы, `init`-only. Изменения через `with`.
- `OpenDocuments[name]: EditorDocument = { Document, Links, NodePositions, VirtualStops }`. **Links, NodePositions и VirtualStops — UI-only**, в бизнес-JSON не уезжают (экспорт пишет только упорядоченный массив `Document.Steps`).
- `WorkflowDocument.Steps` — `ImmutableList<WorkflowStep>`, порядок = бизнес-семантика. ID шага синтетический (`Guid`), `[JsonIgnore]`, существует только в браузерной сессии.
- `SubflowCache: Dict<name, WorkflowDocument>` — кэш ранее открытых/импортированных процессов сессии для отображения шагов внутри `SubflowNodeWidget` и для «проваливания» в subflow. Наполняется при `OpenWorkflowAction`.
- Редьюсеры — чистые: никаких `Guid.NewGuid()` / I/O. Создание GUID для нового документа — в `EditorEffects.HandleCreateWorkflowRequested`.
- Полиморфизм шагов — через абстрактные `WithDescription` и `CloneAsNew`, плюс `WithStepKind` / `WithSubflowName` в наследниках.

---

## Точки расширения

### Новый тип шага

1. Наследник `WorkflowStep` в `WorkflowEditor.Core/Models/Steps/`. Реализовать `WithDescription`, `CloneAsNew` и type-specific метод (см. `BaseStep.WithStepKind`).
2. `WorkflowStepJsonConverter`: добавить ветку дискриминатора в `Read`/`Write` (компилятор не подскажет — есть тест на missing-discriminator).
3. Frontend: `Diagram/Nodes/<New>NodeModel.cs` + `Components/Nodes/<New>NodeWidget.razor`.
4. Зарегистрировать в `CanvasTab.razor`: `Diagram.RegisterComponent<NewNodeModel, NewNodeWidget>()`.
5. Кнопка/пункт меню «Добавить шаг → новый» в `Editor.razor`.

Тесты: `WorkflowStepPolymorphismTests`, `WorkflowDocumentJsonDeserializationTests`.

---

## Тесты и CI

- `dotnet test` — проект **Tests.Client**.
- **Snapshot:** `WorkflowDocumentJsonSnapshotTests` фиксирует канонический вид документа (Verify). При изменении модели/конвертера — `.received.json` появляется рядом с `.verified.json`, нужно осознанно принять.
- **Сериализация:** `WorkflowDocumentJsonDeserializationTests` (включая roundtrip), `BranchAndBreakpoint*`.
- **Store/Topology/Layout:** reducers, effects, BranchLinkBuilder, WorkflowGraphValidator, LinearAutoLayout.
- **CI** — `.github/workflows/ci.yml`: `restore → build (Release) → test`. Артефакт — `test-results.trx`.

---

## Известные ограничения / TODO

- Персистентность между сессиями (сейчас всё в памяти; долговременно — только экспорт в файл).
