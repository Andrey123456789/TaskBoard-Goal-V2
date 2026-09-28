# TaskBoard v2

A single-screen task board for managing tasks across projects and users, built
from the ASP.NET Core + Angular Clean Architecture template. Product behavior is
defined by [`SPECIFICATION.md`](SPECIFICATION.md); approved limits live in
[`CUSTOM_SETTINGS.md`](CUSTOM_SETTINGS.md).

"v2" names the second implementation experiment. It is **not** an API version;
the API has no versioning and all routes live under `/api`.

## Structure

```text
backend/
  TaskBoard.Domain/             Entities (User, Project, TaskItem, TaskRelation) and TaskItemStatus
  TaskBoard.Application/        Application services, workflow rules, repository contracts, DTOs, validators
  TaskBoard.Infrastructure/     EF Core (SQL Server), repositories, Unit of Work, migrations, Development seeding
  TaskBoard.Api/                Controllers, validation filter, ProblemDetails/exception handling, Swagger, CORS
  TaskBoard.Application.Tests/  NUnit unit tests (workflow rules, request validators)
  TaskBoard.IntegrationTests/   NUnit + WebApplicationFactory tests against a real SQL Server database
frontend/
  TaskBoard.Web/                Angular (standalone, zoneless, Angular Material) single-screen dashboard
TaskBoard.slnx
```

## Prerequisites

- .NET SDK 10.0.401 or later 10.0 feature band (see `global.json`)
- SQL Server LocalDB (`(localdb)\MSSQLLocalDB`) or another SQL Server
- Node.js 22.22+ / 24.15+ and npm

## Run locally

1. Create the Development database (applies migrations, including the system
   `Deleted User` record):

   ```bash
   dotnet tool restore
   dotnet ef database update --project backend/TaskBoard.Infrastructure --startup-project backend/TaskBoard.Api
   ```

2. Start the API (Development environment, `http://localhost:5080`):

   ```bash
   dotnet run --project backend/TaskBoard.Api --launch-profile http
   ```

   On startup in Development an empty, migrated database is filled with demo
   data (3 users, 2 projects, tasks in every status, related tasks). Existing
   data is never changed. To get a fresh dataset, drop the database
   (`dotnet ef database drop ...`) and repeat step 1.

   Swagger UI: <http://localhost:5080/swagger>

3. Start the Angular application (`http://localhost:4200`):

   ```bash
   cd frontend/TaskBoard.Web
   npm install
   npm start
   ```

   The development build calls the API at `http://localhost:5080`
   (`src/environments/environment.development.ts`). The API allows that origin
   through `Cors:AllowedOrigins` in `appsettings.Development.json`.

## Tests

```bash
dotnet test TaskBoard.slnx
```

Integration tests create an isolated, uniquely named database on LocalDB, apply
the migrations, and drop it afterwards. To use another SQL Server, set
`TASKBOARD_TESTS_SQLSERVER` to a server connection string (without a database
name). Tests never use the Development database or its seed data.

```bash
cd frontend/TaskBoard.Web
npm test -- --watch=false
```

## Implementation notes

- **Architecture:** simple Domain entities; use-case and workflow rules
  (status transitions, assignment rules, edit/delete locks) live in Application
  services. Services return explicit per-operation outcome types that the
  controllers map to HTTP status codes (`404`, `409`, `422`).
- **Validation:** FluentValidation validators check request contracts and run in
  an API action filter, producing `400` validation problem responses.
- **Deleted User:** a system user with a well-known ID, created by the initial
  migration (not by demo seeding). It is never listed by `/api/users`, cannot be
  addressed through user operations (`404`), and cannot be manually assigned
  (`422`). Task responses mark it with `assignee.isDeletedUser = true`; an
  unassigned task has `assignee: null`.
- **Uniqueness:** user email is unique case-insensitively; project names are
  compared exactly (the specification only makes email case-insensitive). Both
  are enforced by unique indexes with explicit column collations.
- **Related tasks:** stored once per pair; `PUT /api/tasks/{id}/related-tasks`
  replaces the full set atomically and never changes `UpdatedAt`.
- **Errors:** unexpected exceptions are logged once (Serilog) and returned as a
  sanitized `500` ProblemDetails document.
