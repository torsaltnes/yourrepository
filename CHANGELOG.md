# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [Unreleased]

## [FEAT-003] – Employee Competence Profile – 2026-04-27
### Added
- Employee competence profile feature with education, certificate, and course entry management.
- New Angular `/my-profile` page with a polished overview, section cards, and entry forms.
- Backend Minimal API group at `/api/profile` for reading and managing the current employee profile.
- Development Bearer-token authentication wiring so the profile feature resolves identity server-side.

### Changed
- Profile requests now use ASP.NET Core authorization and server-validated claims instead of a client-supplied employee ID header.
- The Angular HTTP layer now sends an `Authorization: Bearer` token automatically for API calls.
- The profile feature follows the existing vertical-slice pattern with in-memory repository-backed persistence.

### Fixed
- Removed the forgeable `X-Employee-Id` trust path that allowed identity spoofing across profile endpoints.

## [FEAT-002] – Deviation / Non-conformity management feature – 2026-04-27
### Added
- Deviation domain model with severity/status enums and reported/updated timestamps.
- In-memory deviation CRUD API under `/api/deviations`.
- Angular deviation feature under `frontend/src/app/deviations/` with list and form flows.
- Route `/deviations` in the Angular app.
- Tailwind CSS v4 styling for the new deviations UI.

### Changed
- Backend deviation endpoints were implemented as a Minimal API endpoint group backed by a singleton in-memory repository.
- Backend JSON serialization was configured to use string enums for readable API payloads.
- Backend CORS was enabled for `http://localhost:4200` and `https://localhost:4200`.
- Frontend state management now uses Angular Signals and Reactive Forms for the deviation feature.
- Frontend development proxy and backend launch settings were aligned for local integration.

### Fixed
- HTTPS redirection is now skipped in development to avoid local SSL issues.
- Integration tests were aligned with string-based enum JSON handling.
