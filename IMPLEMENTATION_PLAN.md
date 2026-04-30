# IMPLEMENTATION_PLAN

## 1. Feature Goal

Deliver FEAT-006 as a production-ready deviation management dashboard that replaces the current seeded/mock dashboard with a responsive, data-driven experience backed by the existing deviations domain and a new dashboard summary API.

## 2. Current-State Findings

### 2.1 Repository and stack
- Monorepo with `frontend/` (Angular 20 + Tailwind CSS 4 + Chart.js/ng2-charts) and `backend/` (.NET minimal API with Application/Domain/Infrastructure layering).
- Root `.gitignore` already exists.
- `frontend/package.json` already includes:
  - `@angular/*` 20.x
  - `chart.js` 4.4.x
  - `ng2-charts` 7.x
  - `tailwindcss` 4.x
  - `@tailwindcss/postcss`
- `backend/src/Greenfield.Api/Program.cs` already follows the expected minimal API pattern:
  - default CORS for `http://localhost:4200` and `https://localhost:4200`
  - `JsonStringEnumConverter`
  - conditional `UseHttpsRedirection()` outside development
  - endpoint registration via `app.MapDeviationEndpoints()`

### 2.2 Existing domain and frontend assets
- Deviations domain/application already exists and should be reused for dashboard data.
- Existing dashboard state is currently seeded/mock (`frontend/src/app/features/dashboard/data/dashboard.store.ts`).
- Existing deviations list/store feature already handles filters, paging, badges, and export; this should be reused for the dashboard table rather than re-inventing list semantics.

### 2.3 Visual manifest implications
The visual manifest and feature analysis point to:
- top bar + sidebar shell
- 4 KPI cards
- dual-chart layout (line/area trend + doughnut/pie status distribution)
- filter/search/date controls
- paginated table on desktop
- mobile fallback to stacked cards with collapsed sidebar
- Tailwind v4 theme tokens using OKLCH values, with a dark sidebar accent and semantic status colors

## 3. Architecture Decisions

1. **Keep the backend split by Clean Architecture layers** and add dashboard-specific query/read-model logic without moving deviation CRUD logic.
2. **Add one new dashboard summary endpoint** for KPI + chart aggregates, while **reusing the existing deviations list endpoint** for the table/filter/pagination experience.
3. **Keep Angular standalone + signals architecture**:
   - standalone components only
   - `inject()` for DI
   - `signal()`, `computed()`, `effect()`, and `resource()`/`rxResource()` for state and async loading
   - built-in control flow (`@if`, `@for`, `@switch`)
4. **Use `ng2-charts` with Chart.js** rather than introducing a new chart library, because both dependencies are already present and align with Angular standalone bootstrap via `provideCharts(withDefaultRegisterables())`.
5. **Use Tailwind CSS 4 CSS-first theming** in the frontend stylesheet with `@theme` + `@source` and no `tailwind.config.js` theme customization.
6. **Do not introduce auth changes**. The dashboard remains accessible without login/guard changes.

## 4. Target Solution Overview

### 4.1 Data flow
- Dashboard page loads two data streams:
  1. `GET /api/dashboard/summary` for KPI cards + trend chart + status distribution
  2. existing deviations list endpoint for the table/filter/pagination data
- Frontend store owns UI filter state and coordinates API calls.
- Backend summary service aggregates from the deviations data source instead of returning client-side mock data.

### 4.2 Page composition
- App shell/top bar/sidebar structure aligned to the visual manifest
- KPI grid (4 cards)
- trend chart card
- status distribution chart card
- recent/paged deviations table
- mobile card-list alternative for narrow screens

## 5. Backend Design

### 5.1 New API contract
Create a dashboard-focused read contract.

**Endpoint**
- `GET /api/dashboard/summary`

**Query parameters**
- `from` (`DateTimeOffset?`): optional lower date bound
- `to` (`DateTimeOffset?`): optional upper date bound

**Response shape**
- `DashboardSummaryDto`
  - `DashboardKpisDto kpis`
  - `DashboardTrendPointDto[] trend`
  - `DashboardStatusSliceDto[] statusDistribution`

**Recommended DTO structure**
- `DashboardSummaryDto`
- `DashboardKpisDto`
  - `total`
  - `open`
  - `underTreatment`
  - `closed`
- `DashboardTrendPointDto`
  - `bucketStart`
  - `openedCount`
  - `closedCount`
- `DashboardStatusSliceDto`
  - `status`
  - `count`
  - `percentage`

Notes:
- Keep the table on the existing deviations endpoint so filter/paging behavior stays consistent with the current deviations feature.
- Serialize enums as strings; `Program.cs` is already configured for this.

### 5.2 Application layer changes
Create a dedicated dashboard query service in the Application layer.

**Responsibilities**
- validate date-range inputs
- request aggregate data from infrastructure/repositories
- map domain aggregation results to DTO records
- enforce consistent period bucketing for the trend chart
- keep library-project awaits on `.ConfigureAwait(false)`

**Recommended files**
- `backend/src/Greenfield.Application/Dashboard/Contracts/DashboardSummaryDto.cs`
- `backend/src/Greenfield.Application/Dashboard/Contracts/DashboardKpisDto.cs`
- `backend/src/Greenfield.Application/Dashboard/Contracts/DashboardTrendPointDto.cs`
- `backend/src/Greenfield.Application/Dashboard/Contracts/DashboardStatusSliceDto.cs`
- `backend/src/Greenfield.Application/Dashboard/Contracts/GetDashboardSummaryQuery.cs`
- `backend/src/Greenfield.Application/Dashboard/IDashboardService.cs`
- `backend/src/Greenfield.Application/Dashboard/DashboardService.cs`

**Implementation notes**
- Use `record` DTOs/query objects.
- Use a `sealed` service class with a primary constructor.
- Keep query logic read-only and separate from mutation use-cases.
- Reuse existing deviation repository abstractions where possible; add read-model methods only where current abstractions cannot efficiently support aggregation.

### 5.3 Infrastructure changes
Add efficient dashboard aggregation support in Infrastructure.

**Required capabilities**
- count deviations by status for current filter range
- compute total deviations and KPI metrics
- group deviations by time bucket for trend chart
- return counts for status distribution

**Preferred approach**
- extend the existing deviation repository/data source abstraction with dedicated aggregation methods instead of materializing all deviations into memory on every request
- if the current project still uses seeded/in-memory data, keep the same abstraction so the implementation can later swap to database-backed grouping without API changes

**Likely file touch points**
- existing deviation repository interface and implementation(s)
- `backend/src/Greenfield.Infrastructure/Extensions/...` service registration file(s)

### 5.4 API layer changes
Add a new minimal API endpoint group for dashboard summary data.

**Recommended files**
- `backend/src/Greenfield.Api/Endpoints/DashboardEndpoints.cs`
- modify `backend/src/Greenfield.Api/Program.cs`

**Endpoint behavior**
- map `/api/dashboard` with `.WithTags("Dashboard")`
- expose `MapGet("/summary", ...)`
- return `TypedResults.Ok(...)` on success
- return `TypedResults.BadRequest(...)` for invalid date ranges (`from > to`, unsupported span if capped)

**Program.cs updates**
- register dashboard services via Application/Infrastructure extensions
- call `app.MapDashboardEndpoints()`
- keep current CORS and enum JSON settings intact

### 5.5 Backend validation rules
- reject invalid date ranges where `from > to`
- standardize trend bucket granularity (weekly or monthly; monthly is the safer default for dashboard readability)
- use UTC/`DateTimeOffset` consistently end-to-end
- do not localize enum/string values in the API; keep transport values stable and let the frontend handle display labels

## 6. Frontend Design

### 6.1 Routing and feature ownership
The new dashboard should replace the current dashboard landing experience rather than coexist with seeded content.

**Recommended file touch points**
- `frontend/src/app/app.routes.ts`
- existing dashboard page entry file(s)

**Routing behavior**
- keep dashboard route unguarded
- lazy-load heavy dashboard feature code if not already lazy-loaded
- preserve navigation path consistency from the visual design/sidebar

### 6.2 State management strategy
Refactor `frontend/src/app/features/dashboard/data/dashboard.store.ts` into a real API-backed signal store.

**Store responsibilities**
- own date-range and status/search UI filters used by the dashboard shell
- load dashboard summary via `rxResource()` or equivalent signal-driven async pattern
- coordinate the existing deviations list/table source for paged rows
- expose `loading`, `error`, and `emptyState` signals
- map API DTOs into chart datasets/options for presentational components

**Signals to maintain**
- `filters`
- `summaryResource` / `summary`
- `tableQuery`
- `selectedStatus`
- `searchTerm`
- `dateRange`
- `isMobileSidebarOpen`
- computed `kpis`, `trendChartData`, `statusChartData`, `recentRows`

### 6.3 Component structure
Create a container/presentational split so the page remains maintainable.

**Recommended frontend files**
- `frontend/src/app/features/dashboard/dashboard.page.ts`
- `frontend/src/app/features/dashboard/data/dashboard-api.service.ts`
- `frontend/src/app/features/dashboard/data/dashboard.models.ts`
- `frontend/src/app/features/dashboard/components/dashboard-shell.component.ts`
- `frontend/src/app/features/dashboard/components/dashboard-filter-bar.component.ts`
- `frontend/src/app/features/dashboard/components/deviation-kpi-card.component.ts`
- `frontend/src/app/features/dashboard/components/deviation-trend-chart.component.ts`
- `frontend/src/app/features/dashboard/components/status-distribution-chart.component.ts`
- `frontend/src/app/features/dashboard/components/recent-deviations-table.component.ts`
- `frontend/src/app/features/dashboard/components/recent-deviations-mobile-list.component.ts`

**Component rules**
- standalone components only
- `ChangeDetectionStrategy.OnPush`
- `inject()` instead of constructor injection
- built-in control flow only
- use `@defer` for chart components if initial bundle size/layout jank becomes noticeable

### 6.4 Reuse of existing deviations feature
Avoid duplicating deviation list logic.

**Preferred reuse path**
- extract or reuse a deviations API/query service so both:
  - `deviation-list.page`
  - dashboard table section
  consume the same backend contract

**If current list store is tightly page-coupled**
- factor HTTP calls into a shared service such as:
  - `frontend/src/app/features/deviations/data/deviation-api.service.ts`
- keep page-specific selection/filter UI logic in each store/page

### 6.5 Chart integration
Use existing `ng2-charts` + Chart.js dependencies.

**Bootstrap requirement**
- update `frontend/src/main.ts` to register chart providers once:
  - `provideCharts(withDefaultRegisterables())`

**Chart design**
- line/area chart for deviation trend over time
  - datasets: opened vs closed counts
  - use soft fill beneath at least one dataset
- doughnut chart for status distribution
  - legend displayed on desktop, simplified or stacked below chart on mobile
- configure `responsive: true`
- allow `maintainAspectRatio: false` inside fixed-height dashboard cards
- centralize color tokens from CSS variables/OKLCH theme mapping

### 6.6 Tailwind CSS 4 and theming
Use Tailwind v4 CSS-first theming in the main stylesheet.

**Required stylesheet updates**
- `frontend/src/styles.css`

**Required contents**
- `@import "tailwindcss";`
- `@source "./src/**/*.ts";`
- `@theme { ... }` with dashboard tokens for:
  - primary purple
  - sidebar surface
  - semantic success/warning/danger/info colors
  - card/background/border/text tokens
  - spacing/radius/shadow tokens as needed

**Design token guidance**
- keep OKLCH values in CSS custom properties
- no hardcoded hex utilities in templates
- use `gap-*`, grid, and responsive variants instead of spacing hacks
- add dark-sidebar treatment via theme tokens rather than ad hoc inline values

### 6.7 Responsive behavior
Implement the responsive rules implied by the sketches.

**Desktop (1280–1440px)**
- persistent sidebar
- 4-column KPI grid
- 2-column charts row
- full data table with pagination/actions

**Tablet**
- 2-column KPI layout
- charts stack if width is constrained
- filters wrap cleanly

**Mobile (375px class)**
- collapsible/drawer sidebar
- KPI cards stack vertically
- charts stack vertically with fixed card heights
- table replaced by card-list representation with key fields + status badge + primary action

### 6.8 UX details
- keep Norwegian-facing labels if the design language requires it, but keep API values language-neutral/stable
- surface loading/skeleton states for cards and charts
- show empty state when no deviations match current filters
- preserve status/severity badge styling consistency with the existing deviations feature

## 7. File-by-File Change Plan

### 7.1 Backend

| File | Action | Purpose |
|---|---|---|
| `backend/src/Greenfield.Api/Program.cs` | Modify | Map dashboard endpoints and ensure service registration is wired |
| `backend/src/Greenfield.Api/Endpoints/DashboardEndpoints.cs` | Create | Minimal API endpoint group for dashboard summary |
| `backend/src/Greenfield.Application/Dashboard/Contracts/DashboardSummaryDto.cs` | Create | Summary response record |
| `backend/src/Greenfield.Application/Dashboard/Contracts/DashboardKpisDto.cs` | Create | KPI record |
| `backend/src/Greenfield.Application/Dashboard/Contracts/DashboardTrendPointDto.cs` | Create | Trend point record |
| `backend/src/Greenfield.Application/Dashboard/Contracts/DashboardStatusSliceDto.cs` | Create | Status distribution record |
| `backend/src/Greenfield.Application/Dashboard/Contracts/GetDashboardSummaryQuery.cs` | Create | Query record for filters/date range |
| `backend/src/Greenfield.Application/Dashboard/IDashboardService.cs` | Create | Dashboard query abstraction |
| `backend/src/Greenfield.Application/Dashboard/DashboardService.cs` | Create | Application service with aggregation orchestration |
| `backend/src/Greenfield.Application/Extensions/...` | Modify | Register dashboard service in DI |
| `backend/src/Greenfield.Infrastructure/...Deviation...Repository...` | Modify | Add aggregation/read-model methods |
| `backend/src/Greenfield.Infrastructure/Extensions/...` | Modify | Register any new repository/query services |
| `backend/tests/Greenfield.Api.Tests/DashboardEndpointsTests.cs` | Create | Integration tests for summary endpoint |
| `backend/tests/Greenfield.Application.Tests/DashboardServiceTests.cs` | Create | Unit tests for KPI/trend/status aggregation mapping |

### 7a. Frontend Contract Updates

The backend introduces a new dashboard summary contract and endpoint. The following frontend files must be created or modified to keep the Angular contract synchronized.

| File | Change |
|---|---|
| `frontend/src/app/features/dashboard/data/dashboard.models.ts` | Create interfaces for `DashboardSummaryDto`, `DashboardKpisDto`, `DashboardTrendPointDto`, and `DashboardStatusSliceDto` |
| `frontend/src/app/features/dashboard/data/dashboard-api.service.ts` | Add `getSummary(query: DashboardSummaryQuery)` method calling `GET /api/dashboard/summary` |
| `frontend/src/app/features/dashboard/data/dashboard.store.ts` | Replace seeded/mock types with imported dashboard contract interfaces; map summary response into KPI, trend, and doughnut chart view models |
| `frontend/src/app/features/dashboard/dashboard.page.ts` | Consume the new summary state and coordinate it with the deviations table source |
| `frontend/src/app/features/dashboard/components/deviation-kpi-card.component.ts` | Accept typed KPI input derived from `DashboardKpisDto` |
| `frontend/src/app/features/dashboard/components/deviation-trend-chart.component.ts` | Accept typed `DashboardTrendPointDto[]`-derived chart input |
| `frontend/src/app/features/dashboard/components/status-distribution-chart.component.ts` | Accept typed `DashboardStatusSliceDto[]`-derived chart input |
| `frontend/src/app/features/deviations/data/deviation-api.service.ts` | If absent, create/extract shared deviations list method so the dashboard table can reuse the existing backend list contract without duplicating HTTP logic |
| `frontend/src/app/app.routes.ts` | Ensure the replacement dashboard route points to the new API-backed dashboard experience |
| `frontend/src/main.ts` | Register `provideCharts(withDefaultRegisterables())` if not already present so typed chart components can render the contract data |

**Type mappings applied**
- `record` DTO -> TypeScript `interface`
- `Guid` -> `string`
- `string` -> `string`
- `int`, `long`, `decimal`, `double` -> `number`
- `bool` -> `boolean`
- `DateTimeOffset`, `DateTime` -> `string` (ISO 8601)
- nullable date/time -> `string | null`
- `IReadOnlyList<T>` / `List<T>` / `T[]` -> `T[]`
- backend enum values -> string-literal union matching serialized enum names

**Dashboard-specific mappings expected here**
- `DashboardTrendPointDto.bucketStart: DateTimeOffset` -> `bucketStart: string`
- `DashboardStatusSliceDto.status: DeviationStatus` -> `status: '...'` string-literal union matching backend serialization
- KPI counts -> `number`

### 7.2 Frontend UI/styling

| File | Action | Purpose |
|---|---|---|
| `frontend/src/main.ts` | Modify | Register Chart.js providers once at bootstrap |
| `frontend/src/styles.css` | Modify | Tailwind v4 `@source` + `@theme` tokens for dashboard and status colors |
| `frontend/src/app/app.routes.ts` | Modify | Route new dashboard as the primary replacement experience |
| `frontend/src/app/features/dashboard/dashboard.page.ts` | Create/replace | Container page for layout, data wiring, responsive sections |
| `frontend/src/app/features/dashboard/data/dashboard.store.ts` | Modify | Replace seeded/mock dashboard state with live API-backed signals |
| `frontend/src/app/features/dashboard/data/dashboard-api.service.ts` | Create | Summary HTTP client |
| `frontend/src/app/features/dashboard/data/dashboard.models.ts` | Create | Dashboard DTO interfaces |
| `frontend/src/app/features/dashboard/components/dashboard-shell.component.ts` | Create | Sidebar/topbar shell composition |
| `frontend/src/app/features/dashboard/components/dashboard-filter-bar.component.ts` | Create | Search/status/date filters |
| `frontend/src/app/features/dashboard/components/deviation-kpi-card.component.ts` | Create | KPI presentation card |
| `frontend/src/app/features/dashboard/components/deviation-trend-chart.component.ts` | Create | Line/area chart wrapper |
| `frontend/src/app/features/dashboard/components/status-distribution-chart.component.ts` | Create | Doughnut chart wrapper |
| `frontend/src/app/features/dashboard/components/recent-deviations-table.component.ts` | Create | Desktop table |
| `frontend/src/app/features/dashboard/components/recent-deviations-mobile-list.component.ts` | Create | Mobile card-list version |
| `frontend/src/app/features/deviations/data/deviation-api.service.ts` | Create/modify | Shared deviation list retrieval for dashboard table reuse |

## 8. Implementation Sequence

1. **Backend contract first**
   - add DTO records/query record
   - implement dashboard service
   - add infrastructure aggregation methods
   - expose `/api/dashboard/summary`
2. **Backend tests**
   - service mapping/aggregation tests
   - endpoint integration tests
3. **Frontend contract sync**
   - add TypeScript interfaces + summary API service
   - extract shared deviations query service if needed
4. **Frontend shell and store refactor**
   - replace seeded dashboard store with API-backed signals
   - register chart providers in bootstrap
5. **Visual implementation**
   - layout shell
   - KPI cards
   - charts
   - table/mobile card list
   - filters
6. **Styling and responsiveness**
   - Tailwind tokens
   - desktop/tablet/mobile layouts
7. **Verification**
   - frontend build
   - backend build
   - responsive/manual QA against the design intent

## 9. Testing Strategy

### 9.1 Backend
- unit test KPI aggregation logic
- unit test trend bucketing logic
- unit test status distribution percentage calculation
- integration test `GET /api/dashboard/summary`
- verify enum JSON serialization remains string-based
- verify bad date range returns `400`

### 9.2 Frontend
- unit test `dashboard.store.ts` state transitions for success/loading/error
- component tests for KPI card/chart/table inputs
- verify chart components render with typed inputs and no runtime registration errors
- verify responsive layout behavior at desktop/tablet/mobile breakpoints
- verify dashboard loads without auth/guard intervention

### 9.3 End-to-end acceptance validation
- dashboard route shows new layout instead of old seeded dashboard
- KPI cards use real API data
- line/area chart shows trend data from API
- doughnut chart shows status distribution from API
- table renders paged deviation rows from real endpoint
- mobile view collapses sidebar and swaps table to cards
- `ng build` and `dotnet build` succeed

## 10. Risks and Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| Existing deviations repository lacks aggregation methods | Medium | Add dedicated read-model methods instead of loading full lists client-side |
| Dashboard store currently mixes mock/demo shape with UI logic | Medium | Separate API contract models from presentation mapping in the refactor |
| Chart provider registration missing in bootstrap | High | Explicitly add `provideCharts(withDefaultRegisterables())` in `main.ts` |
| Tailwind classes missing from Angular inline templates | High | Ensure `@source "./src/**/*.ts";` exists in `frontend/src/styles.css` |
| Enum naming mismatch between backend and frontend unions | Medium | Reuse serialized enum names exactly; do not create translated transport values |
| Mobile table layout becomes unreadable | Medium | Use a dedicated mobile card-list component instead of forcing a dense table |

## 11. Definition of Done

The feature is complete when:
- the new dashboard replaces the current seeded dashboard
- summary data comes from `/api/dashboard/summary`
- table data comes from the existing deviations list contract
- all dashboard visuals are responsive and aligned with the visual manifest/design intent
- Angular 20 patterns (standalone, signals, built-in control flow, `inject()`) are followed
- Tailwind CSS 4 theming is tokenized with OKLCH values
- backend remains minimal API + Clean Architecture compliant
- API/frontend contracts are synchronized and documented in section 7a
- frontend and backend builds pass without errors
