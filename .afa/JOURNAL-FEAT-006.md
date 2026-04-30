# Cognitive Journal

This journal records agent decisions and rationale throughout the pipeline run.
Entries are append-only and timestamped.

## [IntakeAgent] 2026-04-30 08:37:40 UTC

Domain Classification: FullStack (confirmed). Visual analysis revealed two complementary dashboard designs:
1. dashboard-avvik.png (Norwegian "deviation" dashboard): Table-focused layout with KPI cards, filter bar, and tabular data. Colors use oklch color space with primary purple (0.55 0.18 250), success/warning/danger semantic tokens. Viewport 1280x800.
2. design-example_2.png (Template dashboard): Sidebar-based layout with dark sidebar, KPI cards in 4-column grid, dual-chart section (line + donut), and data table. Darker sidebar accent (oklch(0.17 0.025 265)). Viewport 1440x900.
Key scope decisions: Using the more comprehensive design-example_2.png as primary template while integrating deviation-specific domain (from dashboard-avvik.png) such as status badges (Open/In-treatment/Closed) and deviation API integration. Responsive breakpoints defined for desktop (>1024px), tablet (640-1024px), and mobile (<640px). No authentication/authorization layer required per brief. Tailwind v4 with oklch color tokens. Angular + .NET API integration with real deviation data. New branch 'feature/FEAT-006-deviation-dashboard' to be created.

---

## [ArchitectAgent] 2026-04-30 08:46:28 UTC

For FEAT-006 planning, chose a split contract strategy for the dashboard: add a dedicated backend dashboard summary endpoint for KPI/chart aggregates while reusing the existing deviations list contract for the table to minimize churn and preserve pagination/filter semantics. Standardized contract sync as C# record DTO -> TypeScript interface, Guid -> string, DateTimeOffset -> ISO string, numeric scalars -> number, and enums -> string-literal unions matching serialized backend enum names.

---

