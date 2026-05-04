# IMPLEMENTATION_PLAN

## 1. Overview
Build a greenfield browser-based `Vaktliste` module as a lightweight single-page application using Vite + TypeScript + Tailwind CSS.
The module must let a shift manager assign employees to morning or afternoon shifts for specific dates, render the plan in a table-first layout, and block duplicate occupancy of the same date/shift slot.

Model shift dates as validated date-only ISO strings (`YYYY-MM-DD`) throughout the domain and UI state. Create `ShiftDate` values only through `parseShiftDate()` and reject unchecked date strings from forms or rehydration.

State must live behind a small observable store/repository boundary rather than raw in-memory component state. Use an in-memory snapshot backed by `sessionStorage` persistence, rehydrate only validated persisted data during application startup, and surface explicit persistence failures so the UI never silently diverges from storage.

All scheduling mutations must flow through one pure domain function that enforces the duplicate-slot conflict rule and returns either updated state or a structured conflict result. Reuse that single result source for form validation and the error alert. If the table shows `KONFLIKT`, treat it as transient UI state for the latest submit attempt only; do not persist conflict markers as domain data.

Render all user-provided values with safe text APIs (`textContent`/`createTextNode`) rather than raw HTML interpolation.

A minimal application shell and hash-based navigation are required because the workspace currently has no existing frontend or routing infrastructure. Empty hash should resolve to `#/`, while unknown hashes should render a lightweight 404 view with a link back to the overview.

## 2. Folder structure and files to create

### Root
- `package.json` (create)
- `package-lock.json` (create via `npm install`)
- `tsconfig.json` (create)
- `vite.config.ts` (create)
- `vitest.config.ts` (create)
- `index.html` (create)
- `IMPLEMENTATION_PLAN.md` (create)
- `bootstrap.sh` (create)
- `bootstrap.ps1` (create)

### Source
- `src/main.ts` (create)
- `src/app/App.ts` (create)
- `src/app/routes.ts` (create)
- `src/app/router.ts` (create)
- `src/app/shell/AppShell.ts` (create)
- `src/views/OverviewPage.ts` (create)
- `src/views/VaktlistePage.ts` (create)
- `src/components/ScheduleLegend.ts` (create)
- `src/components/ScheduleTable.ts` (create)
- `src/components/ShiftCell.ts` (create)
- `src/components/ShiftBadge.ts` (create)
- `src/components/ShiftAssignmentDialog.ts` (create)
- `src/components/ErrorAlert.ts` (create)
- `src/domain/models.ts` (create)
- `src/domain/ShiftValidationService.ts` (create)
- `src/services/InMemoryScheduleRepository.ts` (create)
- `src/services/ScheduleService.ts` (create)
- `src/services/seedData.ts` (create)
- `src/utils/dateFormatting.ts` (create)
- `src/styles/tokens.css` (create)
- `src/styles/app.css` (create)

### Tests
- `src/test/setup.ts` (create)
- `tests/domain/ShiftValidationService.test.ts` (create)
- `tests/services/ScheduleService.test.ts` (create)
- `tests/components/ScheduleTable.test.ts` (create)
- `tests/views/VaktlistePage.test.ts` (create)
- `tests/app/router.test.ts` (create)

## 2a. Application Shell & Navigation
- Create a minimal shell with a persistent header, app title, top navigation, and content region.
- Navigation items:
  - `Oversikt` → `#/`
  - `Vaktliste` → `#/vaktliste`
- Default behavior:
  - `#/` renders a simple overview/landing page with a short module description and a primary link/button to `#/vaktliste`.
  - Unknown routes redirect to `#/vaktliste` so the feature is always reachable.
- Shell changes:
  - Header title: product-level label such as `Bemanningssystem`.
  - Active-route highlighting in the nav.
  - Breadcrumb/subheading area inside `VaktlistePage` showing `Oversikt / Vaktliste`.
- `VaktlistePage` must include the primary action button `Ny vakt +` in the page footer/action row, opening a dialog/form for new assignments.
- No auth guards are required in the MVP.

## 2b. Visual Requirements
Use the visual manifest as the source of truth for tokens and structure.

### Color palette → CSS custom properties
Define these in `src/styles/tokens.css` and consume them from Tailwind utility classes via arbitrary values or semantic utility wrappers:
- `--color-primary-morning: oklch(0.70 0.12 225)`
- `--color-primary-evening: oklch(0.72 0.15 140)`
- `--color-error: oklch(0.62 0.22 30)`
- `--color-surface-light: oklch(0.98 0.01 0)`
- `--color-surface-header: oklch(0.90 0.02 0)`
- `--color-text-primary: oklch(0.15 0.01 0)`
- `--color-text-secondary: oklch(0.35 0.02 0)`
- `--color-border: oklch(0.85 0.03 0)`

### Component library selection
- Use **Tailwind CSS v4 only** for UI primitives.
- Do **not** introduce a separate component library; the manifest only requires simple primitives (header, badge, table, cards, button, alert), so custom app components are lower-risk and keep the stack framework-neutral.

### Layout strategy
- Overall page layout: vertical flex column matching the manifest (`header` → `table` → `footer`).
- Table container: horizontally scrollable on narrow screens using `overflow-x-auto`.
- Schedule grid: semantic HTML table for accessibility and column alignment.
- Responsive breakpoints:
  - Mobile: stacked header/legend/footer rows.
  - `md` and above: header and footer return to horizontal alignment.
- Sticky table header is recommended for usability when the number of rows grows.

### Visual tokens
- Font family: `-apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif`
- Border radius token: `--radius-md: 0.75rem`
- Spacing scale: normalize to `4 / 8 / 12 / 16 / 24 / 32px`
- Title font size target: `2rem`
- Header font size target: `1.125rem`
- Body and button font size target: `1rem`
- Morning cards/badges: blue-tinted background/text/border using `--color-primary-morning`
- Afternoon cards/badges: green-tinted background/text/border using `--color-primary-evening`
- Conflict card/alert: error-tinted background/text/border using `--color-error`

## 3. Detailed implementation instructions per file
### `src/domain/types.ts`
- Define core types for the scheduling domain.
- Required types:
  - `ShiftType = 'morning' | 'afternoon'`
  - `type ShiftDate = string & { readonly __brand: 'ShiftDate' }`
  - assignment/state/conflict result interfaces used by store and UI
  - persistence error/result interfaces used by repository and store UI feedback
- Export `parseShiftDate(raw: string): ShiftDate | null` as the only supported constructor for `ShiftDate`.
- `parseShiftDate()` must accept only canonical `YYYY-MM-DD` calendar dates and reject unchecked strings.
- Forbid direct `as ShiftDate` casting in application code and tests; create fixtures through `parseShiftDate()` or helpers that call it.
- Keep domain state as date-only values; do not store JS `Date` objects in domain state.

### `src/domain/scheduler.ts`
- Export the single pure domain function for scheduling mutations.
- Required behavior:
  - accept current state plus an assignment command containing a validated `ShiftDate`
  - reject duplicate occupancy of the same `date + shift`
  - return either `{ ok: true, state }` or `{ ok: false, error }`
- The conflict error must be structured (for example code, date, shift, message) so it can drive inline UI and alerts from one source.
- This file is the only place where the duplicate-slot business rule is implemented.

### `src/state/vaktlisteRepository.ts`
- Create a small repository abstraction for persistence.
- Implement a `sessionStorage`-backed repository with a fixed storage key.
- Required behavior:
  - `load()` returns only fully validated persisted state when present
  - validate the entire payload shape before returning it: root object, collections, each employee string, shift enum, and each date via `parseShiftDate()`
  - missing storage key falls back to an empty initial state
  - invalid/malformed JSON or invalid payload shape falls back safely to an empty initial state; do not partially rehydrate best-effort data
  - `save(state)` serializes the full validated scheduler state and returns a discriminated result such as `{ ok: true } | { ok: false, code, message }`
  - map `JSON.stringify`/`sessionStorage.setItem` failures into structured error codes/messages; never silently ignore them.

### `src/state/vaktlisteStore.ts`
- Create the app-facing store/service layer.
- Expose a small observable API such as `getSnapshot()`, `subscribe(listener)`, and mutation methods.
- Required behavior:
  - rehydrate initial state from `vaktlisteRepository` at startup
  - expose read access for the current committed plan
  - expose mutation methods that delegate to `scheduler.ts`
  - when `scheduler.ts` returns success, build a candidate next snapshot, call `repository.save(candidateState)` first, and commit the in-memory snapshot only when `save().ok === true`
  - if `save()` fails, keep the previous committed snapshot, do not commit candidate data, and surface a structured persistence error for UI consumption
  - if `scheduler.ts` returns conflict, keep current committed state unchanged, do not persist, and surface that structured conflict as transient latest-submit status only
  - notify subscribers when committed state changes and when transient latest-submit status changes so the UI can re-render correctly
  - clear transient submit status after the next successful submission or explicit view reset.
- Do not re-implement conflict checks in views.

### `src/views/OverviewPage.ts`
- Class/function name: `OverviewPage`.
- Required output:
  - Short description of the scheduling module.
  - Primary CTA linking to `#/vaktliste`.
- Keep implementation static; do not duplicate scheduling logic here.

### `src/views/VaktlistePage.ts`
- Class/function name: `VaktlistePage`.
- Build the scheduling UI.
- Required behavior:
  - collect employee, shift, and date input
  - keep the date value as the raw `YYYY-MM-DD` string only in local form state
  - on submit, call `parseShiftDate(rawDate)` at the view/application boundary
  - stop submission and show validation feedback when `parseShiftDate()` returns `null`
  - submit mutations through `vaktlisteStore` using only validated `ShiftDate` values
  - show conflict and persistence feedback from the store's structured result model
  - render the table from store state/derived data only.
- Any `KONFLIKT` marker shown in the table must come from the shared scheduler/store result model for the latest submit attempt, not from persisted domain data or view-specific duplicate logic.

### `src/views/NotFoundPage.ts`
- Class/function name: `NotFoundPage`.
- Required output:
  - short 404-style message for unsupported hash routes
  - link back to `#/`.

### `src/router.ts`
- Implement hash-based route resolution.
- Supported routes:
  - `#/` -> `OverviewPage`
  - `#/vaktliste` -> `VaktlistePage`
- Required behavior:
  - empty hash normalizes to `#/`
  - unknown hashes render `NotFoundPage`
  - router is responsible only for navigation/view selection, not scheduling rules.

### `src/domain/scheduler.test.ts`
- Add Vitest coverage for the pure scheduler logic before final UI wiring.
- Minimum test cases:
  - accepts a valid assignment into an empty slot
  - rejects duplicate occupancy for same `date + shift`
  - returns the expected structured conflict payload.
- Construct `ShiftDate` fixtures through `parseShiftDate()` helpers rather than string casts.

### `src/state/vaktlisteRepository.test.ts`
- Add Vitest coverage for persistence behavior.
- Minimum test cases:
  - rehydrates previously saved valid state from `sessionStorage`
  - falls back to empty initial state on missing storage key
  - falls back safely on malformed persisted JSON
  - falls back safely on structurally invalid persisted payload anywhere in the tree
  - returns the expected structured `save()` error when storage writes fail.

### `src/state/vaktlisteStore.test.ts`
- Add Vitest coverage for store semantics.
- Minimum test cases:
  - successful mutation persists candidate state before committing the snapshot
  - save failure leaves the previous snapshot intact and surfaces a persistence error to the UI
  - duplicate occupancy returns transient `KONFLIKT` status without mutating or persisting state
  - `getSnapshot()` exposes only committed state
  - `subscribe()` notifies observers on committed updates and transient submit-status changes, and supports unsubscribe.

### `src/router.test.ts`
- Add Vitest coverage for routing behavior.
- Minimum test cases:
  - empty hash resolves to `#/`
  - known hashes resolve to the expected view
  - unknown hashes render the 404 view.

### `bootstrap.sh`
- Bash bootstrap script for macOS/Linux.
- Required steps:
  - verify `node` and `npm` exist
  - print the detected versions
  - run `npm install`
  - print follow-up commands (`npm run dev`, `npm test`, `npm run build`)
- Use `set -euo pipefail`.

### `bootstrap.ps1`
- PowerShell bootstrap script for Windows.
- Required steps mirror `bootstrap.sh`.
- Fail fast if Node.js is missing.

## 4. Dependencies
Use npm with exact versions (no caret ranges):

### Dev dependencies
- `vite@6.3.5`
- `typescript@5.8.3`
- `tailwindcss@4.1.7`
- `@tailwindcss/vite@4.1.7`
- `vitest@3.2.2`
- `jsdom@26.1.0`
- `@testing-library/dom@10.4.0`
- `@testing-library/user-event@14.6.1`
- `@types/node@22.15.18`

### Notes
- No backend package is required for MVP because state is intentionally in-memory and session-scoped.
- No frontend component library is required because Tailwind-only primitives are sufficient for the supplied visual manifest.

## 5. Automated tests
Create these test files:
- `tests/domain/ShiftValidationService.test.ts`
- `tests/services/ScheduleService.test.ts`
- `tests/components/ScheduleTable.test.ts`
- `tests/views/VaktlistePage.test.ts`
- `tests/app/router.test.ts`

Test coverage expectations:
- Domain rule coverage for conflict prevention.
- Service orchestration coverage for in-memory persistence and subscriber notifications.
- DOM rendering coverage for table layout, badges, conflict state, and alert text.
- Navigation coverage for hash routing and fallback behavior.

## 6. Acceptance criteria
- **AC-001 Shift Assignment Creation**: Fulfilled by `ShiftAssignmentDialog` + `VaktlistePage.handleCreateAssignment()` + `ScheduleService.createAssignment()` creating a new in-memory assignment for a selected employee, date, and shift type.
- **AC-002 Duplicate Shift Prevention**: Fulfilled by `ShiftValidationService`, which rejects any new assignment that targets an already-occupied `date + shiftType` slot and returns a conflict result for UI display.
- **AC-003 Shift Schedule Table Display**: Fulfilled by `ScheduleTable`, which renders employees as rows and formatted date/day columns with shift cards inside the grid.
- **AC-004 Visual Shift Differentiation**: Fulfilled by `ShiftBadge` variant mapping and Tailwind token usage for blue morning cards and green afternoon cards.
- **AC-005 Error Notification**: Fulfilled by `ErrorAlert`, which displays the exact required message after a conflict result is returned.
- **AC-006 In-Memory Data Storage**: Fulfilled by `InMemoryScheduleRepository`, which stores employees, assignments, and conflict state only in runtime memory.
- **AC-007 Tailwind CSS Styling**: Fulfilled by the Vite + Tailwind CSS setup and all page/component styling being implemented with Tailwind utilities plus CSS tokens.
- **AC-008 Add Shift Button**: Fulfilled by the `Ny vakt +` primary action in `VaktlistePage` footer/action row opening the assignment dialog.
- **AC-009 Layout Matches Visual Design**: Fulfilled by the shell/page structure matching the manifest’s header + legend, table grid, and footer alert/action arrangement, with manifest colors/tokens applied.

## 7. Bootstrap scripts
Create both scripts in the repository root.

### `bootstrap.sh`
- Shebang: `#!/usr/bin/env bash`
- Safety: `set -euo pipefail`
- Commands:
  1. Check `node --version`
  2. Check `npm --version`
  3. Run `npm install`
  4. Print next steps:
     - `npm run dev`
     - `npm test`
     - `npm run build`

### `bootstrap.ps1`
- Commands:
  1. Validate `node` is available (`Get-Command node`)
  2. Validate `npm` is available (`Get-Command npm`)
  3. Run `npm install`
  4. Print next steps:
     - `npm run dev`
     - `npm test`
     - `npm run build`

Implementation note: because the repo is currently greenfield, the bootstrap scripts should assume they are executed from the repository root and should not contain backend-specific restore commands.

## Debate Summary

_Adversarial review completed in 3 round(s). Outcome: ⚠️ No consensus._

| Round | Challenger Raised | Resolution |
|-------|-------------------|------------|
| 1 | Kun in-memory state gir datatap ved refresh/crash og gjør modulen skjør utover … | Revised |
| 2 | Store muterer i minne uten robust håndtering av sessionStorage-feil; quota/priv… | Revised |
| 3 | `vaktlisteStore`-seksjonen sier fortsatt «persist after successful updates», so… | Revised |

**⚠️ Risk Note:** `vaktlisteStore`-seksjonen sier fortsatt «persist after successful updates», som kan gjeninnføre divergens mellom minne og `sessionStorage` ved save-feil. (Foreslått alternativ: Gjør save-før-commit eksplisitt i filkontrakten: persister kandidatstate først, commit snapshot kun v…