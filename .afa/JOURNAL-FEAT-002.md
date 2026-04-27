# Cognitive Journal

This journal records agent decisions and rationale throughout the pipeline run.
Entries are append-only and timestamped.

## [IntakeAgent] 2026-04-27 14:18:33 UTC

FEAT-002 Analysis: Feature already well-structured with explicit technical domain (FullStack). Acceptance criteria comprehensive and explicit. No scope creep detected. Validated: Core business value is clear (quality/compliance tracking foundation). Technical stack explicitly stated: .NET 10 backend, Angular 18+ frontend, Tailwind CSS v4, in-memory storage. Domain confidence: HIGH - explicit technology choices + CRUD operations across full stack. No assumptions needed beyond what's stated. Ready for development hand-off.

---

## [ArchitectAgent] 2026-04-27 14:25:46 UTC

Created FEAT-002 implementation plan around the repository’s existing layered architecture, choosing Minimal API endpoint groups instead of controllers to satisfy mandated .NET 10 conventions while preserving the required /api/deviations route contract. Planned a singleton ConcurrentDictionary-backed repository because the requirement explicitly allows in-memory storage and it fits cleanly with scoped application services. Also called out mandatory cross-cutting fixes in Program.cs (development-safe HTTPS handling and Angular localhost CORS) because the current host setup would otherwise block local frontend/backend integration.

---

## [CodingAgent] 2026-04-27 14:40:22 UTC

Implemented FEAT-002 Deviation/Non-conformity Management end-to-end.

KEY DECISIONS:
1. Used Minimal API endpoint group (MapGroup + TypedResults) rather than MVC controller — matches project standard and plan requirement.
2. In-memory repository registered as singleton (ConcurrentDictionary) so data survives scoped-service lifetime; DeviationService registered as scoped.
3. JsonStringEnumConverter applied via ConfigureHttpJsonOptions so the API serializes/deserializes enums as readable strings ("High", "Open") matching Angular binding without client mapping.
4. Program.cs: wrapped UseHttpsRedirection() in !IsDevelopment() guard; added CORS for localhost:4200 (http+https); updated launchSettings to ports 5000/5001; proxy.conf.json updated from 5250→5000 for consistency.
5. Angular: all components are standalone OnPush with signal-based state (signal/computed/effect). Reactive form uses FormBuilder.nonNullable. @for/@if control flow throughout. @angular/forms added to package.json.
6. Integration test fix: PostAsJsonAsync/PutAsJsonAsync must pass JsonOptions with JsonStringEnumConverter, because .NET 10 JsonStringEnumConverter rejects integer enum inputs when reading requests.

RESULTS: 29/29 backend tests passed, 32/32 frontend tests passed, ng build and dotnet build both succeeded with 0 warnings.

---

