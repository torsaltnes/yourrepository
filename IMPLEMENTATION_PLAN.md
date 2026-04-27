# FEAT-003 — Employee Competence Profile

## 1. Goal
Deliver a user-owned competence profile feature that allows authenticated employees to create, view, edit, and delete:
- education entries
- certificate entries
- course entries

The feature must expose a clear grouped overview on the profile page and fit the current full-stack architecture:
- backend: .NET 10 Minimal API with Clean Architecture shape
- frontend: Angular 20 standalone application with Tailwind CSS 4

## 2. Current Codebase Context

### Backend
- Solution already follows `Domain -> Application -> Infrastructure -> Api`.
- API uses Minimal APIs and centralized DI registration.
- Existing persistence appears to be in-memory only.
- Tests already exist at application and API levels.
- JWT/auth claim-based access exists, but no dedicated current-user abstraction was identified.

### Frontend
- Angular `20.3.x` with standalone components and lazy-loaded feature routes.
- Tailwind CSS `4.2` is already installed with CSS import-based setup.
- Existing features are organized under `src/app/features`.
- Current styling approach uses Tailwind utility classes directly in templates.

## 3. Scope and Assumptions
- Feature is user-self-service only: users manage their own competence profile, not other employees’ data.
- The profile overview groups entries by type: Education, Certificates, Courses.
- `skills acquired` for courses should be stored as a string collection, not a single free-text blob, to support future search/filtering.
- Because current infrastructure is in-memory, this plan keeps repository contracts persistence-agnostic and recommends an in-memory implementation for parity plus a clearly isolated path to durable persistence later.
- If production durability is required immediately, replace the in-memory repository with a database-backed implementation before release.

## 4. Architectural Decisions

### AD-1: Model the feature as a profile aggregate with typed child collections
Use a single `CompetenceProfile` aggregate per employee containing three typed collections:
- `EducationEntry`
- `CertificateEntry`
- `CourseEntry`

Rationale:
- matches the UI requirement of a grouped profile overview
- keeps ownership enforcement simple
- allows a single read model for the profile page
- reduces duplication versus three unrelated top-level repositories

### AD-2: Use a dedicated current-user abstraction
Introduce an application/API abstraction such as `ICurrentUserContext` to resolve the authenticated employee identifier from JWT claims.

Rationale:
- removes claim parsing from endpoints and services
- simplifies tests
- keeps ownership rules consistent across all profile operations

### AD-3: Keep CRUD commands typed per entry kind
Expose typed commands/DTOs for education, certificates, and courses instead of a generic polymorphic payload.

Rationale:
- acceptance criteria define distinct fields per entry type
- avoids weak validation and complex discriminators
- keeps Angular forms simpler and explicit

### AD-4: Use Angular signals for UI state, reactive forms for editing
Use signals for page/store state and Reactive Forms for create/edit forms.

Rationale:
- aligns with Angular 20 signal-first guidance
- avoids BehaviorSubject-based local state
- keeps form validation ergonomic for CRUD forms

## 5. Backend Design

### 5.1 Domain Layer
Create a new feature area, e.g. `CompetenceProfiles`, containing:
- `CompetenceProfile` aggregate root
- `EducationEntry`
- `CertificateEntry`
- `CourseEntry`

Recommended fields:

#### CompetenceProfile
- `EmployeeId`
- `IReadOnlyList<EducationEntry>`
- `IReadOnlyList<CertificateEntry>`
- `IReadOnlyList<CourseEntry>`

#### EducationEntry
- `Id`
- `Degree`
- `Institution`
- `GraduationYear`
- `Notes`
- `CreatedUtc`
- `UpdatedUtc`

#### CertificateEntry
- `Id`
- `Name`
- `IssuingOrganization`
- `IssueDate`
- `ExpirationDate?`
- `CreatedUtc`
- `UpdatedUtc`

#### CourseEntry
- `Id`
- `Name`
- `Provider`
- `CompletionDate`
- `IReadOnlyList<string> SkillsAcquired`
- `CreatedUtc`
- `UpdatedUtc`

Domain rules:
- all entry IDs should be stable GUIDs
- profile ownership is defined by `EmployeeId`
- `GraduationYear` must be within a valid range
- `ExpirationDate` cannot be earlier than `IssueDate`
- `SkillsAcquired` must ignore blank values and be normalized/trimmed
- update operations must preserve entry identity and timestamps correctly

### 5.2 Application Layer
Add:
- repository abstraction: `ICompetenceProfileRepository`
- service abstraction: `ICompetenceProfileService`
- DTO records for read/write operations
- request/response records for all CRUD operations

Recommended service surface:
- `GetMyProfileAsync(employeeId)`
- `AddEducationAsync(employeeId, request)`
- `UpdateEducationAsync(employeeId, entryId, request)`
- `DeleteEducationAsync(employeeId, entryId)`
- equivalent methods for certificates and courses

Read model:
- return a grouped `CompetenceProfileDto` containing three sorted collections
- suggested sorting:
  - education: `GraduationYear desc`
  - certificates: `IssueDate desc`
  - courses: `CompletionDate desc`

Validation should live in application/domain boundaries, not only in the UI.
Use record types for all DTOs and request models.
Use `ConfigureAwait(false)` on awaited calls in non-API projects.

### 5.3 Infrastructure Layer
Add an implementation of `ICompetenceProfileRepository`.

#### Preferred near-term implementation
- `InMemoryCompetenceProfileRepository`
- storage keyed by `EmployeeId`
- thread-safe collection, e.g. `ConcurrentDictionary`

#### Future-ready design requirement
The repository contract must not leak in-memory assumptions so it can later be replaced by a database-backed implementation without changing application services.

Persistence risk note:
- in-memory storage is acceptable for current project parity and local development
- it is not sufficient for production employee profiles because data will be lost on restart

### 5.4 API Layer
Create a new endpoint group, for example:
- `/api/me/competence-profile`

Recommended endpoints:
- `GET /api/me/competence-profile`
- `POST /api/me/competence-profile/education`
- `PUT /api/me/competence-profile/education/{entryId}`
- `DELETE /api/me/competence-profile/education/{entryId}`
- `POST /api/me/competence-profile/certificates`
- `PUT /api/me/competence-profile/certificates/{entryId}`
- `DELETE /api/me/competence-profile/certificates/{entryId}`
- `POST /api/me/competence-profile/courses`
- `PUT /api/me/competence-profile/courses/{entryId}`
- `DELETE /api/me/competence-profile/courses/{entryId}`

Implementation guidance:
- map endpoints with `MapGroup()` and `.WithTags("Competence Profile")`
- return `TypedResults.Ok`, `TypedResults.Created`, `TypedResults.NoContent`, `TypedResults.NotFound`, `TypedResults.ValidationProblem` as appropriate
- resolve authenticated user via `ICurrentUserContext`
- mark endpoints as authenticated if the existing API does not already do this globally
- keep endpoint handlers thin; delegate business logic to application services

HTTP behavior:
- `GET` returns the grouped profile payload
- `POST` returns created entry DTO plus location metadata if route naming is already used
- `PUT` returns updated profile or updated entry DTO
- `DELETE` returns `204 No Content`
- operations against non-existent entry IDs return `404`

### 5.5 Validation Rules

#### Education
- `Degree`: required, trimmed, max length defined centrally
- `Institution`: required, trimmed, max length defined centrally
- `GraduationYear`: required, reasonable range (e.g. 1900..current year + 1)
- `Notes`: optional, bounded length

#### Certificate
- `Name`: required
- `IssuingOrganization`: required
- `IssueDate`: required
- `ExpirationDate`: optional, must be `>= IssueDate`

#### Course
- `Name`: required
- `Provider`: required
- `CompletionDate`: required
- `SkillsAcquired`: optional but normalized into a distinct trimmed list

## 6. Frontend Design

### 6.1 Feature Structure
Create a new feature folder under `frontend/src/app/features`, e.g.:
- `competence-profile/`
  - page component
  - feature store/service
  - form components
  - section/list components
  - API client models

Recommended component split:
- `CompetenceProfilePageComponent` — routed container
- `CompetenceOverviewSectionComponent` — reusable section shell
- `EducationEntryFormComponent`
- `CertificateEntryFormComponent`
- `CourseEntryFormComponent`
- `CompetenceEntryCardComponent` or type-specific card components if layouts diverge

### 6.2 Routing
Add a lazy-loaded standalone route, for example:
- `/profile/competence`

Route should use `loadComponent` or feature route file consistent with current Angular route organization.

### 6.3 State Management
Use a signal-based feature store/service, e.g. `CompetenceProfileStore`, with:
- `profile = signal<CompetenceProfileViewModel | null>(null)`
- `loading = signal(false)`
- `saving = signal(false)`
- `error = signal<string | null>(null)`
- `hasEntries = computed(...)`

Responsibilities:
- load grouped profile data from API
- submit create/update/delete actions
- optimistically update local state only if current project patterns allow it; otherwise refresh after mutations
- normalize server responses for the page

Do not use `BehaviorSubject` for page state.
Use `inject()` for `HttpClient`, router utilities, and any services.

### 6.4 Forms
Use Angular Reactive Forms inside standalone form components.
Signals should manage form mode and visibility state:
- create vs edit mode
- selected entry ID
- dialog/drawer open state

Form UX requirements:
- field-level validation messages
- disabled submit while invalid or saving
- cancel/reset behavior
- clear success/failure feedback at page level

Suggested form-specific behavior:
- Education: numeric year input
- Certificate: date inputs with optional expiration date
- Course: skills editor that converts chip-like input or comma-separated text into a normalized string array

### 6.5 Page Composition
The profile page should render:
- page header with feature summary
- grouped sections for education, certificates, and courses
- add button per section
- empty state when a section has no entries
- card/list presentation for each entry with edit/delete affordances

Template guidance:
- use `@if`, `@else`, and `@for`
- use `ChangeDetectionStrategy.OnPush`
- prefer signal-based `input()`/`output()` for child component I/O
- use `@defer` only if the final component composition becomes heavy enough to justify deferred rendering

### 6.6 API Integration
Create a feature-scoped API client service, e.g. `CompetenceProfileApiClient`, `providedIn: 'root'`.

Client methods:
- `getProfile()`
- `addEducation(request)` / `updateEducation(...)` / `deleteEducation(...)`
- same for certificates and courses

Use typed interfaces matching backend records.
Keep all transformation logic localized to the feature store or mapping helpers.

## 7. Styling and UX

### 7.1 Tailwind CSS 4 Alignment
Follow Tailwind 4 CSS-first rules:
- continue using `@import "tailwindcss"`
- define any new semantic tokens in `@theme` blocks inside CSS, not `tailwind.config.js`
- reuse existing design tokens where possible
- add feature-specific tokens only if the current design system lacks required spacing/color/surface primitives

### 7.2 Visual Structure
Recommended layout:
- single-column mobile layout
- wider desktop container with section cards/panels
- consistent spacing via `gap-*`
- section headers with clear hierarchy and actions aligned responsively
- tags/chips for course skills
- subtle empty-state and form-status messaging

### 7.3 Accessibility
- semantic headings per grouped section
- keyboard-accessible add/edit/delete controls
- visible focus states
- labels for all form controls
- validation messages tied to controls
- avoid color-only status indicators

## 8. Data Contracts

### 8.1 Response DTO
`CompetenceProfileDto`
- `education: EducationEntryDto[]`
- `certificates: CertificateEntryDto[]`
- `courses: CourseEntryDto[]`

### 8.2 Request DTOs
- `CreateEducationRequest`
- `UpdateEducationRequest`
- `CreateCertificateRequest`
- `UpdateCertificateRequest`
- `CreateCourseRequest`
- `UpdateCourseRequest`

All request/response models should be C# `record` types.
Frontend TypeScript interfaces should mirror the API contracts closely.

## 9. Security and Ownership
- all endpoints must require authentication
- employee identity must come from token claims, never from client-submitted employee IDs
- no endpoint should allow cross-user access
- logs must avoid sensitive or unnecessary personal data
- delete/update operations must first verify the entry belongs to the authenticated user’s profile

## 10. Testing Strategy

### 10.1 Backend Unit Tests
Add application tests for:
- creating profile entries when no profile exists yet
- editing an existing education/certificate/course entry
- deleting entries
- validation failures for invalid dates/year/blank required fields
- skills normalization for courses
- sorted grouped read model behavior

### 10.2 Backend API Integration Tests
Using `WebApplicationFactory<TEntryPoint>`:
- authenticated `GET` returns empty grouped profile for new user
- authenticated `POST` creates each entry type successfully
- authenticated `PUT` updates entry
- authenticated `DELETE` removes entry
- invalid payload returns validation error response
- unauthenticated request returns `401`
- user A cannot affect user B’s data

### 10.3 Frontend Tests
Add Angular/Vitest tests for:
- grouped rendering of sections
- empty-state rendering
- create/edit/delete interaction flow
- form validation behavior
- store state transitions (`loading`, `saving`, `error`, successful reload)
- skills parsing/normalization UI logic

## 11. Suggested File Plan

### Backend
- `backend/src/Domain/CompetenceProfiles/...`
- `backend/src/Application/CompetenceProfiles/...`
- `backend/src/Infrastructure/CompetenceProfiles/InMemoryCompetenceProfileRepository.cs`
- `backend/src/Api/Endpoints/CompetenceProfileEndpoints.cs`
- `backend/src/Api/Extensions/ServiceCollectionExtensions.cs` (registration update)
- `backend/tests/Application.Tests/CompetenceProfiles/...`
- `backend/tests/Api.Tests/CompetenceProfiles/...`

### Frontend
- `frontend/src/app/features/competence-profile/...`
- route registration update in the app routing area
- optional shared UI component updates if a generic section/card shell is reused
- stylesheet token update only if required by missing design-system tokens

## 12. Delivery Sequence
1. Add domain models and repository contract.
2. Add application DTOs, service, validation, and mapping.
3. Add in-memory infrastructure implementation and DI registration.
4. Add Minimal API endpoints and authentication/current-user wiring.
5. Add backend unit and integration tests.
6. Add Angular route, API client, and signal-based feature store.
7. Add grouped page and form components.
8. Apply Tailwind styling aligned with the existing design system.
9. Add frontend tests.
10. Validate full CRUD flow end-to-end.

## 13. Risks and Follow-ups
- **Primary risk:** in-memory persistence does not satisfy real employee-profile durability expectations.
- **Secondary risk:** current auth setup may not yet expose a stable employee identifier claim; confirm claim source before implementation.
- **UI risk:** if the design system lacks reusable card/form primitives, some additional shared UI work may be needed.
- **Follow-up candidate:** extend this feature later with search/filtering and manager/project discovery views once competence data becomes durable and queryable.
