# Greenfield Architecture

Full-stack employee portal built with .NET 10 Minimal APIs and Angular 20.

## Technical stack

- Backend: .NET 10, ASP.NET Core Minimal APIs, Clean Architecture shape
- Frontend: Angular 20, standalone components, reactive forms, signals
- Styling: Tailwind CSS 4
- Tests: xUnit, FluentAssertions, Vitest
- Persistence: in-memory repositories

## Installation

### Prerequisites

- .NET 10 SDK
- Node.js 20+ and npm

### Backend

```bash
cd backend
dotnet restore
dotnet test
```

Run the API:

```bash
cd backend
dotnet run --project src/GreenfieldArchitecture.Api
```

### Frontend

```bash
cd frontend
npm install
npm test
npm run build
```

Run the Angular app:

```bash
cd frontend
npm start
```

The frontend is configured to proxy API requests to the backend via `proxy.conf.json`.

## API overview

### Employee competence profile

Base path: `/api/me/competence-profile`

All competence profile endpoints require authentication and operate on the current user.

- `GET /api/me/competence-profile` — get the signed-in employee's grouped profile
- `POST /api/me/competence-profile/education` — add an education entry
- `PUT /api/me/competence-profile/education/{entryId}` — update an education entry
- `DELETE /api/me/competence-profile/education/{entryId}` — delete an education entry
- `POST /api/me/competence-profile/certificates` — add a certificate entry
- `PUT /api/me/competence-profile/certificates/{entryId}` — update a certificate entry
- `DELETE /api/me/competence-profile/certificates/{entryId}` — delete a certificate entry
- `POST /api/me/competence-profile/courses` — add a course entry
- `PUT /api/me/competence-profile/courses/{entryId}` — update a course entry
- `DELETE /api/me/competence-profile/courses/{entryId}` — delete a course entry

### Frontend route

- `/profile/competence` — competence profile page

## Notes

- Competence profile data is currently stored in-memory.
- The competence profile page groups education, certificates, and courses into separate sections.
