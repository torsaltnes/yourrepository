# IMPLEMENTATION_PLAN.md

## 1. Overview
Initialize a greenfield full-stack foundation with a Clean Architecture .NET 10 Web API in `backend/` and an Angular standalone application in `frontend/`.
The backend exposes a compilable `GET /api/health` controller using C# primary constructor syntax.
The frontend is scaffolded with `bootstrapApplication`, standalone components, and a signal-driven health feature.
The structure is intentionally minimal, with no database, authentication, Swagger, or business features beyond health status.

## 2. Folder structure and files to create
```text
.gitignore                                                           (create)

backend/GreenfieldArchitecture.sln                                   (create)
backend/Directory.Build.props                                        (create)
backend/Directory.Packages.props                                     (create)

backend/src/Core/Greenfield.Domain/Greenfield.Domain.csproj          (create)
backend/src/Core/Greenfield.Domain/AssemblyReference.cs              (create)

backend/src/Core/Greenfield.Application/Greenfield.Application.csproj (create)
backend/src/Core/Greenfield.Application/DependencyInjection.cs       (create)
backend/src/Core/Greenfield.Application/Abstractions/Services/IHealthCheckService.cs (create)
backend/src/Core/Greenfield.Application/Health/HealthCheckResult.cs  (create)

backend/src/Infrastructure/Greenfield.Infrastructure/Greenfield.Infrastructure.csproj (create)
backend/src/Infrastructure/Greenfield.Infrastructure/DependencyInjection.cs (create)
backend/src/Infrastructure/Greenfield.Infrastructure/Services/SystemHealthCheckService.cs (create)

backend/src/Api/Greenfield.Api/Greenfield.Api.csproj                (create)
backend/src/Api/Greenfield.Api/Program.cs                           (create)
backend/src/Api/Greenfield.Api/appsettings.json                     (create)
backend/src/Api/Greenfield.Api/appsettings.Development.json         (create)
backend/src/Api/Greenfield.Api/Controllers/HealthCheckController.cs (create)
backend/src/Api/Greenfield.Api/Contracts/HealthCheckResponse.cs     (create)

backend/tests/Greenfield.UnitTests/Greenfield.UnitTests.csproj      (create)
backend/tests/Greenfield.UnitTests/Controllers/HealthCheckControllerTests.cs (create)
backend/tests/Greenfield.UnitTests/Services/SystemHealthCheckServiceTests.cs (create)

frontend/package.json                                               (create)
frontend/angular.json                                               (create)
frontend/tsconfig.json                                              (create)
frontend/tsconfig.app.json                                          (create)
frontend/tsconfig.spec.json                                         (create)
frontend/src/index.html                                             (create)
frontend/src/main.ts                                                (create)
frontend/src/styles.css                                             (create)
frontend/src/app/app.config.ts                                      (create)
frontend/src/app/app.routes.ts                                      (create)
frontend/src/app/app.component.ts                                   (create)
frontend/src/app/app.component.html                                 (create)
frontend/src/app/app.component.css                                  (create)
frontend/src/app/app.component.spec.ts                              (create)
frontend/src/app/core/models/health-status.model.ts                 (create)
frontend/src/app/core/services/health-api.service.ts                (create)
frontend/src/app/features/health/health-status.component.ts         (create)
frontend/src/app/features/health/health-status.component.html       (create)
frontend/src/app/features/health/health-status.component.css        (create)
frontend/src/app/features/health/health-status.component.spec.ts    (create)
```

## 3. Detailed implementation instructions per file

### Root
- `.gitignore`
  - Ignore `backend/**/bin/`, `backend/**/obj/`, `frontend/node_modules/`, `frontend/dist/`, `frontend/.angular/`, `frontend/coverage/`.
  - Pattern: repository-level ignore file; do not create nested Git repositories.

### Backend solution and shared configuration
- `backend/GreenfieldArchitecture.sln`
  - Solution containing four projects in this order: Domain, Application, Infrastructure, Api, plus UnitTests.
  - Ensure project references reflect Clean Architecture boundaries.

- `backend/Directory.Build.props`
  - Centralize common MSBuild settings: `TargetFramework=net10.0`, `Nullable=enable`, `ImplicitUsings=enable`, `LangVersion=latest`, `TreatWarningsAsErrors=false`.
  - Pattern: shared SDK configuration, not feature logic.

- `backend/Directory.Packages.props`
  - Centralize package versions for test-only dependencies.
  - Add exact versions from section 4.
  - Pattern: Central Package Management.

### Backend Domain layer
- `backend/src/Core/Greenfield.Domain/Greenfield.Domain.csproj`
  - SDK-style class library with no external package references.
  - Must not reference Application, Infrastructure, or Api.

- `backend/src/Core/Greenfield.Domain/AssemblyReference.cs`
  - Class name: `AssemblyReference`.
  - No methods or properties; use as a minimal domain assembly marker.
  - Pattern: future-proof Clean Architecture placeholder with zero framework coupling.

### Backend Application layer
- `backend/src/Core/Greenfield.Application/Greenfield.Application.csproj`
  - SDK-style class library.
  - Reference `Greenfield.Domain` only.

- `backend/src/Core/Greenfield.Application/DependencyInjection.cs`
  - Class name: `DependencyInjection`.
  - Method: `IServiceCollection AddApplication(this IServiceCollection services)`.
  - Return the same `services` instance; keep the application composition root ready for future use cases.
  - Pattern: layer-owned service registration extension.

- `backend/src/Core/Greenfield.Application/Abstractions/Services/IHealthCheckService.cs`
  - Interface name: `IHealthCheckService`.
  - Method: `Task<HealthCheckResult> GetCurrentAsync(CancellationToken cancellationToken)`.
  - Pattern: application abstraction; no ASP.NET types.

- `backend/src/Core/Greenfield.Application/Health/HealthCheckResult.cs`
  - Record name: `HealthCheckResult`.
  - Properties: `string Status`, `string Environment`, `DateTimeOffset CheckedAtUtc`.
  - Pattern: immutable application DTO.

### Backend Infrastructure layer
- `backend/src/Infrastructure/Greenfield.Infrastructure/Greenfield.Infrastructure.csproj`
  - SDK-style class library.
  - Reference `Greenfield.Application`.
  - No database packages in this phase.

- `backend/src/Infrastructure/Greenfield.Infrastructure/DependencyInjection.cs`
  - Class name: `DependencyInjection`.
  - Method: `IServiceCollection AddInfrastructure(this IServiceCollection services)`.
  - Register `TimeProvider.System` as singleton and `IHealthCheckService` -> `SystemHealthCheckService` as scoped.
  - Pattern: infrastructure-only wiring.

- `backend/src/Infrastructure/Greenfield.Infrastructure/Services/SystemHealthCheckService.cs`
  - Class name: `SystemHealthCheckService`.
  - Use a primary constructor: `(TimeProvider timeProvider, IHostEnvironment hostEnvironment)`.
  - Implement `IHealthCheckService`.
  - Method: `GetCurrentAsync(CancellationToken cancellationToken)` returning a `HealthCheckResult` with a static healthy state, current environment name, and UTC timestamp from `timeProvider`.
  - Pattern: infrastructure adapter; no controller logic here.

### Backend API layer
- `backend/src/Api/Greenfield.Api/Greenfield.Api.csproj`
  - Use `Microsoft.NET.Sdk.Web`.
  - Reference `Greenfield.Application` and `Greenfield.Infrastructure`.
  - No Swagger/OpenAPI packages in this phase.

- `backend/src/Api/Greenfield.Api/Program.cs`
  - Configure `builder.Services.AddControllers()`.
  - Call `AddApplication()` and `AddInfrastructure()`.
  - Build app, enable `UseHttpsRedirection()`, and call `MapControllers()`.
  - Add `public partial class Program { }` at file end for future host-based testing extensibility.
  - Pattern: composition root only; no endpoint logic in `Program.cs`.

- `backend/src/Api/Greenfield.Api/appsettings.json`
  - Minimal logging configuration and allowed hosts.
  - No connection strings.

- `backend/src/Api/Greenfield.Api/appsettings.Development.json`
  - Development-only logging overrides.
  - Keep environment configuration minimal.

- `backend/src/Api/Greenfield.Api/Controllers/HealthCheckController.cs`
  - Class name: `HealthCheckController`.
  - Use a primary constructor: `(IHealthCheckService healthCheckService)`.
  - Inherit from `ControllerBase`.
  - Attributes: `[ApiController]` and `[Route("api/health")]`.
  - Method: `[HttpGet] Task<ActionResult<HealthCheckResponse>> Get(CancellationToken cancellationToken)`.
  - Map `HealthCheckResult` to `HealthCheckResponse` and return `Ok(...)`.
  - Pattern: thin controller, application abstraction injected directly, no business logic.

- `backend/src/Api/Greenfield.Api/Contracts/HealthCheckResponse.cs`
  - Record name: `HealthCheckResponse`.
  - Properties: `string Status`, `string Environment`, `DateTimeOffset CheckedAtUtc`.
  - Pattern: API contract isolated from internal application types.

### Backend tests
- `backend/tests/Greenfield.UnitTests/Greenfield.UnitTests.csproj`
  - SDK-style test project.
  - Reference `Greenfield.Api`, `Greenfield.Application`, and `Greenfield.Infrastructure`.
  - Add package references from `Directory.Packages.props`.
  - Set `IsPackable=false`.

- `backend/tests/Greenfield.UnitTests/Controllers/HealthCheckControllerTests.cs`
  - Test class name: `HealthCheckControllerTests`.
  - Use `Moq` to mock `IHealthCheckService`.
  - Test cases:
    - `Get_ReturnsOkObjectResult`.
    - `Get_MapsApplicationResult_ToApiContract`.
    - `Get_PassesCancellationToken_ToService`.
  - Pattern: isolated controller unit tests, no web host.

- `backend/tests/Greenfield.UnitTests/Services/SystemHealthCheckServiceTests.cs`
  - Test class name: `SystemHealthCheckServiceTests`.
  - Use `Moq` to mock `TimeProvider` and `IHostEnvironment`.
  - Test cases:
    - `GetCurrentAsync_ReturnsHealthyStatus`.
    - `GetCurrentAsync_UsesEnvironmentName_FromHostEnvironment`.
    - `GetCurrentAsync_UsesUtcNow_FromTimeProvider`.
  - Pattern: deterministic infrastructure unit tests.

### Frontend workspace and configuration
- `frontend/package.json`
  - Define scripts: `start`, `build`, `test`, `watch`.
  - Include Angular runtime packages, CLI/build tooling, and Vitest/jsdom test tooling from section 4.
  - Pattern: workspace-local toolchain; no global Angular CLI dependency assumed.

- `frontend/angular.json`
  - Configure one application project rooted at `src/`.
  - Build target should output to `dist/frontend`.
  - Test target must use `@angular/build:unit-test`.
  - Pattern: standalone Angular workspace; no NgModule generation assumptions.

- `frontend/tsconfig.json`
  - Base TypeScript configuration with strict mode enabled.
  - Keep path aliases out of scope for this phase.

- `frontend/tsconfig.app.json`
  - App-specific compilation config for `src/main.ts` and application code.

- `frontend/tsconfig.spec.json`
  - Include `src/**/*.spec.ts`.
  - Add test types for `vitest/globals` and `node`.
  - Pattern: dedicated unit-test TypeScript config.

- `frontend/src/index.html`
  - Minimal host page with root element for Angular bootstrapping.

- `frontend/src/main.ts`
  - Bootstrap with `bootstrapApplication(AppComponent, appConfig)`.
  - No `NgModule` usage.
  - Pattern: standalone bootstrap, per Angular current guidance.

- `frontend/src/styles.css`
  - Minimal global styles only; keep feature styling local.

### Frontend application shell
- `frontend/src/app/app.config.ts`
  - Export `appConfig` of type `ApplicationConfig`.
  - Register `provideRouter(routes)` and `provideHttpClient()`.
  - Pattern: provider configuration separated from component code.

- `frontend/src/app/app.routes.ts`
  - Export `routes: Routes`.
  - Define a single default route that lazy-loads `HealthStatusComponent` with `loadComponent`.
  - Pattern: standalone lazy route configuration.

- `frontend/src/app/app.component.ts`
  - Component name: `AppComponent`.
  - Set `standalone: true`.
  - Import `RouterOutlet`.
  - Keep class lightweight; shell only.
  - Pattern: root shell component with no NgModule.

- `frontend/src/app/app.component.html`
  - Render application heading and `<router-outlet />`.
  - Keep markup minimal.

- `frontend/src/app/app.component.css`
  - Minimal layout styling for the shell.

- `frontend/src/app/app.component.spec.ts`
  - Test class/suite: `AppComponent` spec.
  - Configure `TestBed` with `imports: [AppComponent]` and router providers.
  - Test cases:
    - component creates successfully,
    - shell heading renders,
    - router outlet host is present.
  - Pattern: standalone component test using current Angular TestBed imports approach.

### Frontend health feature
- `frontend/src/app/core/models/health-status.model.ts`
  - Interface name: `HealthStatus`.
  - Properties: `status`, `environment`, `checkedAtUtc`.
  - Pattern: frontend transport model matching API response.

- `frontend/src/app/core/services/health-api.service.ts`
  - Service name: `HealthApiService`.
  - Use `inject(HttpClient)` instead of constructor injection.
  - Method: `getHealthStatus()` returning `Observable<HealthStatus>` from `GET /api/health`.
  - Pattern: thin HTTP service; no state stored here.

- `frontend/src/app/features/health/health-status.component.ts`
  - Component name: `HealthStatusComponent`.
  - Set `standalone: true`.
  - Use signals for reactive state: `isLoading`, `health`, `errorMessage`.
  - Add a `computed` property such as `statusLabel` for display text.
  - Method: `runHealthCheck()` that flips loading state, calls `HealthApiService`, updates signals on success/error.
  - Pattern: standalone feature component with local signal-based state; no NgModule.

- `frontend/src/app/features/health/health-status.component.html`
  - Render current signal-driven state.
  - Include a button to invoke `runHealthCheck()`.
  - Show status details when available and error feedback when applicable.

- `frontend/src/app/features/health/health-status.component.css`
  - Local styling only for card/layout/status states.

- `frontend/src/app/features/health/health-status.component.spec.ts`
  - Test suite: `HealthStatusComponent` spec.
  - Configure `TestBed` with `imports: [HealthStatusComponent]` and a mocked `HealthApiService`.
  - Test cases:
    - initial idle state renders,
    - successful service response updates signals and DOM,
    - failed service response shows error state,
    - button disables or reflects loading state while request is in progress.
  - Pattern: component-first testing for signal state transitions.

## 4. Dependencies

### Backend NuGet packages
Use Central Package Management in `backend/Directory.Packages.props`.

- `Microsoft.NET.Test.Sdk` - `17.13.0`
- `xunit.v3` - `2.0.3`
- `xunit.runner.visualstudio` - `3.1.1`
- `Moq` - `4.20.72`

### Backend shared framework
No extra runtime NuGet packages are required for the production API in this phase.
Use the .NET 10 shared framework via `Microsoft.NET.Sdk.Web` and `net10.0`.

### Frontend npm packages
`dependencies`
- `@angular/common`
- `@angular/compiler`
- `@angular/core`
- `@angular/platform-browser`
- `@angular/router`
- `rxjs`
- `tslib`
- `zone.js`

`devDependencies`
- `@angular/build`
- `@angular/cli`
- `@angular/compiler-cli`
- `@types/node`
- `jsdom`
- `typescript`
- `vitest`

## 5. Automated tests

### .NET test files
- `backend/tests/Greenfield.UnitTests/Controllers/HealthCheckControllerTests.cs`
- `backend/tests/Greenfield.UnitTests/Services/SystemHealthCheckServiceTests.cs`

### Angular test files
- `frontend/src/app/app.component.spec.ts`
- `frontend/src/app/features/health/health-status.component.spec.ts`

### What the implementation developer must test
- Backend controller returns HTTP 200 for `GET /api/health`.
- Backend controller maps the application result to the API contract without leaking internal types.
- Backend controller passes the received cancellation token to the application service.
- Infrastructure health service always returns a healthy baseline payload with deterministic environment and timestamp values under test.
- Angular root shell bootstraps as a standalone component and renders without NgModules.
- Angular health component uses signals to drive UI state transitions.
- Angular health component correctly renders success and error states based on the mocked API service.
- `dotnet test backend/GreenfieldArchitecture.sln` and `npm test` must both pass.

## 6. Acceptance criteria
- **AC-001:** A .NET 10 Web API project exists in `/backend`.
  - Fulfilled by `backend/GreenfieldArchitecture.sln` and `backend/src/Api/Greenfield.Api/Greenfield.Api.csproj`, targeting `net10.0` and buildable via `dotnet build backend`.

- **AC-002:** Backend API includes a HealthCheck controller that responds to GET requests.
  - Fulfilled by `backend/src/Api/Greenfield.Api/Controllers/HealthCheckController.cs` exposing `[HttpGet]` on `api/health`.

- **AC-003:** HealthCheck controller uses Primary Constructors.
  - Fulfilled by declaring `HealthCheckController(IHealthCheckService healthCheckService) : ControllerBase`.

- **AC-004:** An Angular 18+ application with Standalone Components exists in `/frontend`.
  - Fulfilled by `frontend/angular.json`, `frontend/package.json`, and `frontend/src/main.ts` bootstrapping a standalone app.

- **AC-005:** Frontend uses Standalone Components with no NgModule required for the root app.
  - Fulfilled by `AppComponent` and `HealthStatusComponent` both using `standalone: true`, with `bootstrapApplication` in `main.ts`.

- **AC-006:** Frontend uses Angular Signals for reactive state management.
  - Fulfilled by `HealthStatusComponent` implementing signal-backed state (`isLoading`, `health`, `errorMessage`) and a computed display value.

- **AC-007:** Both backend and frontend projects compile without errors.
  - Fulfilled when `dotnet build backend/GreenfieldArchitecture.sln` and `npm run build` in `frontend/` both succeed.

- **AC-008:** Changes are pushed to a new feature branch in the origin repository.
  - Fulfilled operationally by creating `feature/INIT-001-greenfield`, committing the scaffold, and pushing with upstream tracking after local build/test success.
