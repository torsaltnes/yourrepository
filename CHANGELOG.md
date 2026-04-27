# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [Unreleased]

## [FEAT-003] – Employee Competence Profile – 2026-04-27
### Added
- Full-stack employee competence profile support for authenticated users.
- Education, certificate, and course management endpoints under `/api/me/competence-profile`.
- Angular competence profile page at `/profile/competence`.
- In-memory competence profile repository and current-user context abstraction.
### Changed
- Competence profile ownership now resolves from the authenticated `NameIdentifier` claim.
- Course skills are normalized on both the client and server to keep stored values consistent.
### Fixed
- API request handling now rejects undefined enum values instead of accepting numeric strings.
- Course skill normalization now ignores null entries to avoid server-side failures.
