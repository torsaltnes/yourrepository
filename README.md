# Greenfield Architecture

Full-stack deviations and employee competence profile demo built with .NET 10 and Angular 20.

## Technical stack

- **Backend:** ASP.NET Core Web API on .NET 10
- **API style:** Minimal APIs with OpenAPI support
- **Authentication (development):** server-validated Bearer tokens via a custom DevApiKey scheme
- **Frontend:** Angular 20 standalone components with lazy-loaded routes
- **State/UI:** Angular Signals, Reactive Forms, Router, HttpClient
- **Styling:** Tailwind CSS v4
- **Storage:** In-memory repositories backed by `ConcurrentDictionary` for deviations and competence profiles
- **Tests:** xUnit for backend, Vitest for frontend

## Installation

### Prerequisites

- .NET 10 SDK
- Node.js and npm

### Backend

```bash
cd backend
dotnet restore GreenfieldArchitecture.sln
dotnet build GreenfieldArchitecture.sln
dotnet test GreenfieldArchitecture.sln
```

### Frontend

```bash
cd frontend
npm install
npm run build
npm run test
```

### Run locally

```bash
# Terminal 1
cd backend/src/GreenfieldArchitecture.Api
dotnet run

# Terminal 2
cd frontend
npm start
```

The frontend runs on `http://localhost:4200` and proxies API calls to the backend during development. Profile requests use the configured development Bearer token automatically; for manual API calls, use one of the tokens in `backend/src/GreenfieldArchitecture.Api/appsettings.Development.json`.

## API overview

The API exposes two feature areas:

### Deviations

The deviation feature is exposed through a Minimal API endpoint group at `/api/deviations`.

- `GET /api/deviations` — list all deviations
- `GET /api/deviations/{id}` — get one deviation
- `POST /api/deviations` — create a deviation
- `PUT /api/deviations/{id}` — update a deviation
- `DELETE /api/deviations/{id}` — delete a deviation

### Employee competence profile

The employee competence profile feature is exposed through a Minimal API endpoint group at `/api/profile` and the Angular route `/my-profile`.

- `GET /api/profile` — get the current employee profile
- `POST /api/profile/education` — create an education entry
- `PUT /api/profile/education/{educationId}` — update an education entry
- `DELETE /api/profile/education/{educationId}` — delete an education entry
- `POST /api/profile/certificates` — create a certificate entry
- `PUT /api/profile/certificates/{certificateId}` — update a certificate entry
- `DELETE /api/profile/certificates/{certificateId}` — delete a certificate entry
- `POST /api/profile/courses` — create a course entry
- `PUT /api/profile/courses/{courseId}` — update a course entry
- `DELETE /api/profile/courses/{courseId}` — delete a course entry

The frontend routes are `/deviations` and `/my-profile`.
