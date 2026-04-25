# IMPLEMENTATION_PLAN

## 1. Overview
Align the existing deviation-management slice with FEAT-002 by normalizing the data model to the required 8 fields, hardening the .NET 10 CRUD API, and polishing the Angular standalone `/deviations` UI.
The repository is not greenfield: most backend/frontend scaffolding already exists, so implementation is primarily targeted refactoring plus missing test coverage.
Preserve Clean Architecture on the backend and Angular standalone components + Signals on the frontend.

## 2. Folder structure and files to create
```text
backend/DeviationManagement.sln                                                          (create)
backend/src/DeviationManagement.Api/DeviationManagement.Api.csproj                       (modify)
backend/src/DeviationManagement.Api/appsettings.Development.json                         (modify)
backend/src/DeviationManagement.Domain/Entities/Deviation.cs                             (modify)
backend/src/DeviationManagement.Application/DTOs/DeviationDto.cs                         (modify)
backend/src/DeviationManagement.Application/DTOs/SaveDeviationRequest.cs                 (modify)
backend/src/DeviationManagement.Application/Services/DeviationService.cs                 (modify)
backend/src/DeviationManagement.Application/Validation/DeviationValidator.cs             (modify)
backend/src/DeviationManagement.Infrastructure/Persistence/InMemory/InMemoryDeviationRepository.cs (modify)
backend/src/DeviationManagement.Api/Contracts/Requests/SaveDeviationApiRequest.cs        (modify)
backend/src/DeviationManagement.Api/Contracts/Responses/DeviationApiResponse.cs          (modify)
backend/src/DeviationManagement.Api/Mapping/DeviationApiMapper.cs                        (modify)
backend/src/DeviationManagement.Api/Controllers/DeviationsController.cs                  (modify)
backend/src/DeviationManagement.Api/Program.cs                                           (modify)
backend/tests/DeviationManagement.UnitTests/DeviationManagement.UnitTests.csproj         (modify)
backend/tests/DeviationManagement.UnitTests/Application/Validation/DeviationValidatorTests.cs (modify)
backend/tests/DeviationManagement.UnitTests/Application/Services/DeviationServiceTests.cs (modify)
backend/tests/DeviationManagement.UnitTests/Api/Controllers/DeviationsControllerTests.cs (modify)
backend/tests/DeviationManagement.UnitTests/Api/DeviationsApiIntegrationTests.cs         (create)

frontend/package.json                                                                    (modify)
frontend/angular.json                                                                    (modify)
frontend/postcss.config.mjs                                                              (modify)
frontend/tsconfig.spec.json                                                              (modify)
frontend/karma.conf.js                                                                   (modify)
frontend/src/styles.css                                                                  (modify)
frontend/src/environments/environment.ts                                                 (modify)
frontend/src/environments/environment.development.ts                                     (modify)
frontend/src/main.ts                                                                     (modify)
frontend/src/app/app.config.ts                                                           (modify)
frontend/src/app/app.routes.ts                                                           (modify)
frontend/src/app/app.routes.spec.ts                                                      (create)
frontend/src/app/core/models/deviation.model.ts                                          (modify)
frontend/src/app/core/models/deviation-form.model.ts                                     (modify)
frontend/src/app/core/services/deviation-api.service.ts                                  (modify)
frontend/src/app/core/services/deviation-api.service.spec.ts                             (modify)
frontend/src/app/features/deviations/data/deviation.store.ts                             (modify)
frontend/src/app/features/deviations/data/deviation.store.spec.ts                        (create)
frontend/src/app/shared/ui/deviation-badge/deviation-badge.component.ts                  (modify)
frontend/src/app/shared/ui/deviation-badge/deviation-badge.component.html                (modify)
frontend/src/app/features/deviations/deviation-list/deviation-list.component.ts          (modify)
frontend/src/app/features/deviations/deviation-list/deviation-list.component.html        (modify)
frontend/src/app/features/deviations/deviation-list/deviation-list.component.spec.ts     (modify)
frontend/src/app/features/deviations/deviation-form/deviation-form.component.ts          (modify)
frontend/src/app/features/deviations/deviation-form/deviation-form.component.html        (modify)
frontend/src/app/features/deviations/deviation-form/deviation-form.component.spec.ts     (modify)
```

## 3. Detailed implementation instructions per file

### Backend
| File | Class / artifact | Implementation instructions |
|---|---|---|
| `backend/DeviationManagement.sln` | Solution file | Create a solution that includes `DeviationManagement.Domain`, `DeviationManagement.Application`, `DeviationManagement.Infrastructure`, `DeviationManagement.Api`, and `DeviationManagement.UnitTests` so the feature can be built/tested from one entry point. |
| `backend/src/DeviationManagement.Api/DeviationManagement.Api.csproj` | API project | Keep `net10.0`, nullable, and implicit usings. Ensure `Microsoft.AspNetCore.OpenApi` stays on `10.0.0`. Verify project references point only inward to Application/Infrastructure/Domain to preserve Clean Architecture. |
| `backend/src/DeviationManagement.Api/appsettings.Development.json` | Development config | Add/confirm `Cors:AllowedOrigins` contains `http://localhost:4200`. Do not use wildcard origins. |
| `backend/src/DeviationManagement.Domain/Entities/Deviation.cs` | `Deviation` entity | Refactor the entity to the exact AC data model: `Id`, `Title`, `Description`, `Severity`, `Status`, `ReportedBy`, `ReportedAt`, `UpdatedAt`. Remove `OccurredAt` and `CreatedAt`. Keep private setters, domain update behavior, and a constructor/update method that refreshes `UpdatedAt`. |
| `backend/src/DeviationManagement.Application/DTOs/DeviationDto.cs` | `DeviationDto` | Match the exact 8-field contract used by the API. No `CreatedAt`; no `OccurredAt`. |
| `backend/src/DeviationManagement.Application/DTOs/SaveDeviationRequest.cs` | `SaveDeviationRequest` | Keep it as an immutable record containing only editable fields: `Title`, `Description`, `Severity`, `Status`, `ReportedBy`, `ReportedAt`. |
| `backend/src/DeviationManagement.Application/Services/DeviationService.cs` | `DeviationService` | Keep the primary constructor. Methods: `GetAllAsync`, `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`. Validate input before persistence, create IDs in the service, set `UpdatedAt = DateTimeOffset.UtcNow` on create/update, preserve in-memory behavior, and map entities to DTOs. Prefer returning newest items first by `ReportedAt` or `UpdatedAt`. |
| `backend/src/DeviationManagement.Application/Validation/DeviationValidator.cs` | `DeviationValidator` | Keep a single `ValidateForSave` method returning `Dictionary<string,string[]>?`. Validate required title/reportedBy/reportedAt, max lengths, and valid enum values. Error keys must stay frontend-friendly (`title`, `description`, `severity`, `status`, `reportedBy`, `reportedAt`). |
| `backend/src/DeviationManagement.Infrastructure/Persistence/InMemory/InMemoryDeviationRepository.cs` | `InMemoryDeviationRepository` | Continue using `ConcurrentDictionary<Guid, Deviation>` with async CRUD methods. Preserve singleton-backed in-memory storage. Update clone/copy logic to the new 8-field entity and return deterministic ordering for the list endpoint. |
| `backend/src/DeviationManagement.Api/Contracts/Requests/SaveDeviationApiRequest.cs` | API request contract | Match the frontend payload exactly, including `reportedAt`. Keep enum types strongly typed so string-enum JSON conversion works consistently. |
| `backend/src/DeviationManagement.Api/Contracts/Responses/DeviationApiResponse.cs` | API response contract | Expose exactly the 8 accepted response properties. Keep `UpdatedAt` for list/detail views. |
| `backend/src/DeviationManagement.Api/Mapping/DeviationApiMapper.cs` | `DeviationApiMapper` | Update all mapper methods to translate `reportedAt`/`updatedAt` correctly across API ↔ Application contracts. Remove all `OccurredAt` / `CreatedAt` mapping. |
| `backend/src/DeviationManagement.Api/Controllers/DeviationsController.cs` | `DeviationsController` | Keep the primary constructor. Implement 5 controller actions: list, get-by-id, create, update, delete. Continue using `[ApiController]` and `[Route("api/deviations")]`. Return `CreatedAtAction` on create, `NoContent` on delete, `ValidationProblemDetails` for 400s, and explicit `ProblemDetails` bodies for 404s instead of empty `NotFound()` responses. Pass cancellation tokens through every call. |
| `backend/src/DeviationManagement.Api/Program.cs` | API bootstrap | Keep controller-based API setup. Configure `AddControllers()` with `JsonStringEnumConverter`, `AddProblemDetails()`, `AddOpenApi()`, named CORS policy for `http://localhost:4200`, `UseExceptionHandler()`, `UseStatusCodePages()`, `UseCors("FrontendPolicy")`, and `MapControllers()`. Preserve `partial class Program` for integration tests. |
| `backend/tests/DeviationManagement.UnitTests/DeviationManagement.UnitTests.csproj` | Test project | Keep/confirm package references for xUnit, Moq, `Microsoft.NET.Test.Sdk`, `Microsoft.AspNetCore.Mvc.Testing`, and `coverlet.collector`. Ensure the project references the API project so `WebApplicationFactory<Program>` integration tests compile. |
| `backend/tests/DeviationManagement.UnitTests/Application/Validation/DeviationValidatorTests.cs` | validator tests | Update tests to the `ReportedAt` field. Cover: required fields, max lengths, invalid enum values, invalid default date, and the valid happy path. Use `[Theory]` where multiple invalid inputs are checked. |
| `backend/tests/DeviationManagement.UnitTests/Application/Services/DeviationServiceTests.cs` | service tests | Cover create/update/delete/list/get-by-id behavior, `UpdatedAt` refresh on update, validation failure path, not-found update/delete path, and correct DTO mapping after the field rename. Mock the repository with Moq. |
| `backend/tests/DeviationManagement.UnitTests/Api/Controllers/DeviationsControllerTests.cs` | controller tests | Verify status codes and response shapes for all 5 endpoints. Add assertions that 400 responses serialize validation errors as `ValidationProblemDetails` and 404 responses return `ProblemDetails` payloads. |
| `backend/tests/DeviationManagement.UnitTests/Api/DeviationsApiIntegrationTests.cs` | API integration tests | Add end-to-end xUnit tests using `WebApplicationFactory<Program>`: full CRUD flow, enum string serialization, validation response format, and CORS behavior for an `Origin: http://localhost:4200` request. Instantiate a fresh factory per test or reset state to avoid singleton store bleed. |

### Frontend
| File | Class / artifact | Implementation instructions |
|---|---|---|
| `frontend/package.json` | npm manifest | Keep the existing Angular 21/Tailwind 4 stack (it satisfies Angular 18+). Add missing `@angular/platform-browser-dynamic` with version `^21.2.10` because `src/test.ts` uses `@angular/platform-browser-dynamic/testing`. Keep the existing `start`, `build`, and headless `test` scripts. |
| `frontend/angular.json` | Angular workspace config | Confirm the application remains standalone-based, `src/styles.css` is loaded globally, and the Karma test target points to `karma.conf.js` and `tsconfig.spec.json`. Do not reintroduce `NgModule`-based scaffolding. |
| `frontend/postcss.config.mjs` | PostCSS config | Ensure Tailwind v4 is wired through `@tailwindcss/postcss`. No legacy Tailwind v3 config file is needed unless the build proves otherwise. |
| `frontend/tsconfig.spec.json` | TS test config | Ensure Jasmine typings are declared and spec compilation includes `src/test.ts` plus `src/**/*.spec.ts`. Keep settings compatible with standalone component tests. |
| `frontend/karma.conf.js` | Test runner config | Keep Jasmine + Karma + ChromeHeadless. Ensure single-run CI execution and coverage output remain enabled so `npm test` is deterministic. |
| `frontend/src/styles.css` | Global styles | Keep `@import "tailwindcss";` per Tailwind v4. Expand shared utility classes for page shell, cards, buttons, form controls, alert banners, and badge variants. Add responsive table helpers for the deviation list and avoid component-scoped design duplication. |
| `frontend/src/environments/environment.ts` | production env | Keep `apiBaseUrl: '/api'` so same-origin builds proxy correctly behind the API host. |
| `frontend/src/environments/environment.development.ts` | dev env | Keep `apiBaseUrl: 'http://localhost:5000/api'` so local Angular dev server hits the backend directly. |
| `frontend/src/main.ts` | Angular bootstrap | Keep `bootstrapApplication(AppComponent, appConfig)`. Do not introduce `AppModule`. |
| `frontend/src/app/app.config.ts` | application config | Keep `provideRouter(routes)` and `provideHttpClient()`. Standalone providers must remain in config, not in an NgModule. |
| `frontend/src/app/app.routes.ts` | route map | Keep standalone `loadComponent` routes for `/deviations`, `/deviations/new`, and `/deviations/:id/edit`. Keep `''` redirecting to `/deviations` and add a wildcard redirect back to `/deviations` for invalid URLs. |
| `frontend/src/app/app.routes.spec.ts` | route tests | Add route-level tests using `provideRouter(routes)` and `RouterTestingHarness` to prove the default redirect and the deviation create/edit routes resolve correctly. |
| `frontend/src/app/core/models/deviation.model.ts` | `Deviation` interface | Normalize the frontend model to the exact 8-property contract: `id`, `title`, `description`, `severity`, `status`, `reportedBy`, `reportedAt`, `updatedAt`. Keep string union types for severity/status. |
| `frontend/src/app/core/models/deviation-form.model.ts` | `DeviationForm` interface | Match the editable payload only: `title`, `description`, `severity`, `status`, `reportedBy`, `reportedAt`. Use a date-string shape compatible with `<input type="date">`. |
| `frontend/src/app/core/services/deviation-api.service.ts` | `DeviationApiService` | Keep a thin HttpClient wrapper with methods `getAll`, `getById`, `create`, `update`, `delete`. Use `environment.apiBaseUrl` and typed observables only; keep view-state concerns in the store. |
| `frontend/src/app/core/services/deviation-api.service.spec.ts` | service tests | Use `provideHttpClient()` before `provideHttpClientTesting()` per Angular docs. Verify all 5 HTTP calls, URL shapes, payload property names (`reportedAt`), and delete/no-content behavior. |
| `frontend/src/app/features/deviations/data/deviation.store.ts` | `DeviationStore` | Keep Signals-based state: `deviations`, `selectedDeviation`, `loading`, `saving`, `error`; computed: `hasItems`, `isEmpty`. Update all field names to `reportedAt`. Add a `clearSelection()` helper for new-form mode. Convert backend `ProblemDetails`/`ValidationProblemDetails` responses into readable messages for the UI. |
| `frontend/src/app/features/deviations/data/deviation.store.spec.ts` | store tests | Add tests for signal state transitions during load/create/update/delete, selected item patching, empty-state computation, and error-message handling from backend problem responses. |
| `frontend/src/app/shared/ui/deviation-badge/deviation-badge.component.ts` | `DeviationBadgeComponent` | Keep the component standalone and `OnPush`. Expose `label` and `kind` inputs, compute CSS classes from severity/status values, and add a computed display label so `InProgress` renders as `In Progress`. |
| `frontend/src/app/shared/ui/deviation-badge/deviation-badge.component.html` | badge template | Render the computed human-readable label and computed class only; keep template dumb and styling-driven. |
| `frontend/src/app/features/deviations/deviation-list/deviation-list.component.ts` | `DeviationListComponent` | Keep it standalone and change detection `OnPush`. Responsibilities: trigger `store.loadAll()` on init, expose `trackById`, handle retry, and call `store.remove()` after confirmation. |
| `frontend/src/app/features/deviations/deviation-list/deviation-list.component.html` | list template | Replace the current card-only presentation with a responsive table/card hybrid: table for desktop, stacked rows/cards for small screens. Show columns for title, severity, status, reported by, reported at, updated at, and actions. Keep loading, error, and empty states. Use `app-deviation-badge` for color coding. |
| `frontend/src/app/features/deviations/deviation-list/deviation-list.component.spec.ts` | list component tests | Verify row rendering, empty state, retry/error state, correct badge labels, and delete action wiring. Include at least one assertion that the routed edit/create links point to `/deviations/...`. |
| `frontend/src/app/features/deviations/deviation-form/deviation-form.component.ts` | `DeviationFormComponent` | Keep it standalone, `OnPush`, and based on `ReactiveFormsModule`. Rename the form control to `reportedAt`. Use route params to detect edit mode, load existing data from the store, patch via `effect`, mark all controls touched on invalid submit, and navigate back to `/deviations` after successful create/update/cancel. |
| `frontend/src/app/features/deviations/deviation-form/deviation-form.component.html` | form template | Build a polished create/edit form with inline validation for all required fields, responsive two-column layout where appropriate, error banner, and disabled submit button during save. Field labels and messages must use `Reported At`, not `Occurred At`. |
| `frontend/src/app/features/deviations/deviation-form/deviation-form.component.spec.ts` | form component tests | Cover create mode, edit mode patching, required/maxlength validation, successful create/update navigation, cancel navigation, and surfaced backend error messages. |

## 4. Dependencies

### Backend (.NET / NuGet)
| Package | Version | Usage |
|---|---:|---|
| `Microsoft.AspNetCore.OpenApi` | `10.0.0` | OpenAPI support for the API project |
| `Microsoft.NET.Test.Sdk` | `17.12.0` | .NET test runner infrastructure |
| `xunit` | `2.9.3` | Unit/integration test framework |
| `xunit.runner.visualstudio` | `2.8.2` | IDE / `dotnet test` discovery |
| `Moq` | `4.20.72` | Mocking repository/service collaborators |
| `coverlet.collector` | `6.0.2` | Coverage collection |
| `Microsoft.AspNetCore.Mvc.Testing` | `10.0.0` | `WebApplicationFactory<Program>` integration tests |

### Frontend (npm)
Keep or add these exact packages in `frontend/package.json`:

**dependencies**
- `@angular/common` `^21.2.10`
- `@angular/compiler` `^21.2.10`
- `@angular/core` `^21.2.10`
- `@angular/forms` `^21.2.10`
- `@angular/platform-browser` `^21.2.10`
- `@angular/platform-browser-dynamic` `^21.2.10` **(add)**
- `@angular/router` `^21.2.10`
- `rxjs` `^7.8.2`
- `tslib` `^2.8.1`
- `zone.js` `^0.16.0`

**devDependencies**
- `@angular-devkit/build-angular` `^21.2.8`
- `@angular/cli` `^21.2.8`
- `@angular/compiler-cli` `^21.2.10`
- `@types/jasmine` `^6.0.0`
- `jasmine-core` `^6.2.0`
- `karma` `^6.4.4`
- `karma-chrome-launcher` `^3.2.0`
- `karma-coverage` `^2.2.1`
- `karma-jasmine` `^5.1.0`
- `karma-jasmine-html-reporter` `^2.2.0`
- `tailwindcss` `^4.1.4`
- `@tailwindcss/postcss` `^4.1.4`
- `postcss` `^8.5.0`
- `typescript` `~5.9.3`

## 5. Automated tests

### Backend test files
- `backend/tests/DeviationManagement.UnitTests/Application/Validation/DeviationValidatorTests.cs` (modify)
  - Test required title/reportedBy/reportedAt.
  - Test max length validation.
  - Test invalid severity/status values.
  - Test valid request returns no errors.
- `backend/tests/DeviationManagement.UnitTests/Application/Services/DeviationServiceTests.cs` (modify)
  - Test create assigns `Id` and `UpdatedAt`.
  - Test update changes `UpdatedAt` and persists renamed fields.
  - Test get/list mapping and ordering.
  - Test validation failure and not-found branches.
  - Test delete success/failure.
- `backend/tests/DeviationManagement.UnitTests/Api/Controllers/DeviationsControllerTests.cs` (modify)
  - Test all 5 endpoint actions.
  - Test `CreatedAtAction` route values.
  - Test 400 validation responses return `ValidationProblemDetails`.
  - Test 404 responses return `ProblemDetails`.
- `backend/tests/DeviationManagement.UnitTests/Api/DeviationsApiIntegrationTests.cs` (create)
  - Test end-to-end CRUD through HTTP.
  - Test enum string serialization.
  - Test `application/problem+json` validation shape.
  - Test CORS headers for `http://localhost:4200`.

### Frontend test files
- `frontend/src/app/app.routes.spec.ts` (create)
  - Test default redirect to `/deviations`.
  - Test new/edit routes resolve to the standalone form component.
- `frontend/src/app/core/services/deviation-api.service.spec.ts` (modify)
  - Test exact CRUD request URLs and payloads.
  - Test `provideHttpClient()` + `provideHttpClientTesting()` setup order.
- `frontend/src/app/features/deviations/data/deviation.store.spec.ts` (create)
  - Test signal state transitions for load/create/update/delete.
  - Test selected deviation behavior.
  - Test error propagation from backend problem responses.
- `frontend/src/app/features/deviations/deviation-list/deviation-list.component.spec.ts` (modify)
  - Test table/card rendering, empty state, retry flow, and delete wiring.
  - Test severity/status badge rendering.
- `frontend/src/app/features/deviations/deviation-form/deviation-form.component.spec.ts` (modify)
  - Test create mode validation.
  - Test edit mode prefill.
  - Test successful create/update navigation to `/deviations`.
  - Test cancel navigation and error banner rendering.

### Build / test verification
- Backend: `dotnet build backend/DeviationManagement.sln` and `dotnet test backend/tests/DeviationManagement.UnitTests/DeviationManagement.UnitTests.csproj`
- Frontend: `npm run build` and `npm test -- --watch=false --browsers=ChromeHeadless` (or the existing `npm test` script if unchanged)

## 6. Acceptance criteria
| AC | Requirement | How the implementation fulfills it |
|---|---|---|
| AC-001 | Deviation with 8 properties | Backend/domain, DTOs, API contracts, and frontend models are normalized to `Id`, `Title`, `Description`, `Severity`, `Status`, `ReportedBy`, `ReportedAt`, `UpdatedAt`. |
| AC-002 | 5 CRUD endpoints + in-memory storage | `DeviationsController`, `DeviationService`, and `InMemoryDeviationRepository` provide GET list/detail, POST, PUT, DELETE using singleton in-memory persistence. |
| AC-003 | Standalone Angular components with Signals | `main.ts` + `app.config.ts` keep `bootstrapApplication`, `provideRouter`, and `provideHttpClient`; `DeviationStore` remains signal-based; list/form remain standalone components. |
| AC-004 | Responsive list with colour-coded badges | `DeviationListComponent` renders responsive table/card views and uses `DeviationBadgeComponent` for severity/status colour coding. |
| AC-005 | Create/Edit form with validation and navigation | `DeviationFormComponent` uses Reactive Forms, inline validation, edit-mode patching, and navigates back to `/deviations` after save/cancel. |
| AC-006 | Tailwind CSS v4 professional polish | Global styles keep Tailwind v4 `@import "tailwindcss";` and shared utilities for cards, buttons, inputs, alerts, and badges. |
| AC-007 | CORS + `/deviations` routing | Backend CORS policy explicitly allows `http://localhost:4200`; frontend routes redirect to and expose `/deviations`, `/deviations/new`, and `/deviations/:id/edit`. |
| AC-008 | Zero-error compilation | The plan includes solution/workspace config updates plus explicit backend/frontend build and test commands that must pass before completion. |
| AC-009 | Push to `feature/FEAT-002-deviations` | Final implementation step: commit validated changes and push them to the `feature/FEAT-002-deviations` branch after all tests pass. |
