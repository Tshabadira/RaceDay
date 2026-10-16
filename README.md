# RaceDay

A full-stack, API-driven event management platform for South Africa's road running, walking and cycling community. Event Organisers create and manage events, categories and participant results. Participants browse upcoming events, enter events, track their personal performance history, and prepare for race day using live weather and route information.

This repository holds **Part 1** (planning and database) and **Part 2** (the RESTful API). Part 3 (the MVC web application) comes later.

## The two roles

**Organiser**
- Creates, edits and deletes **their own** events
- Adds, edits and deletes categories (distances) for their events
- Views every enrolment for their events
- Captures and corrects participant results (finish time and positions)

**Participant**
- Registers an account and logs in
- Browses events and categories, and views route and live weather information
- Enters an event by choosing a category
- Views their own enrolments and their own personal results history

Both roles can view and update their own profile. Role rules are enforced **inside the API** (not just hidden in a screen): a Participant calling an Organiser endpoint receives `403 Forbidden`, and a request without a valid token to a protected endpoint receives `401 Unauthorized`.

## Repository structure

```
RaceDay
├── docs                      Part 1: ERD.pdf, APIEndpointPlan.pdf, RaceDayDatabase.sql
├── Part2
│   └── RaceDay.API           Part 2: the API solution
│       ├── RaceDay.API       The ASP.NET Core Web API project
│       └── RaceDay.Tests     The xUnit unit and integration tests
├── .github/workflows         validate.yml (Part 1 file check), dotnet.yml (build + tests)
└── README.md
```

## Setup and run (Part 2)

**You need:** the .NET 9 SDK, SQL Server (Express is fine) and Visual Studio 2022 or the `dotnet` command line.

1. **Clone** the repository.
2. **Set the two private settings** with user secrets, so no password or key is stored in GitHub. From `Part2/RaceDay.API/RaceDay.API`:
   ```
   dotnet user-secrets set "Jwt:Key" "any-random-text-of-at-least-32-characters"
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=YOUR-SERVER\SQLEXPRESS;Database=RaceDayDataBase;Trusted_Connection=True;TrustServerCertificate=True;"
   ```
   (In Visual Studio you can instead right-click the project and choose **Manage User Secrets**.)
3. **Run** the API (F5 in Visual Studio, or `dotnet run --launch-profile https`). In Development the database and its tables are created automatically from the EF Core migrations.
4. **Open Swagger:** `https://localhost:7015/swagger`
5. **Try the protected endpoints:** call `POST /api/auth/register`, then `POST /api/auth/login`, copy the `token`, press **Authorize** and paste it.

### Run the tests

```
dotnet test
```

The tests start the real API in memory with an empty in-memory database, so they never touch SQL Server.

## How login and roles work

Logging in returns a signed **JWT** (JSON Web Token) that carries the user's ID and role and expires after 2 hours. The client sends it in the `Authorization: Bearer <token>` header on every request, so the API keeps no server-side session. Passwords are **hashed** (ASP.NET Core `PasswordHasher`, with a random salt) and never stored or returned in plain text. Endpoints use `[Authorize(Roles = "Organiser")]` or `[Authorize(Roles = "Participant")]`, and Organiser actions on events, categories and results also check that the event belongs to the logged-in Organiser.

## Endpoints

| Area | Endpoint | Who |
|---|---|---|
| Auth | `POST /api/auth/register`, `POST /api/auth/login` | Everyone |
| Auth | `POST /api/auth/logout` | Logged-in user |
| Profile | `GET` and `PUT /api/profile` | Logged-in user |
| Events | `GET /api/events`, `GET /api/events/{id}` | Everyone |
| Events | `POST /api/events`, `PUT` and `DELETE /api/events/{id}` | Organiser (owner) |
| Weather | `GET /api/events/{id}/weather` | Everyone |
| Categories | `GET /api/events/{id}/categories`, `GET /api/categories/{id}/route` | Everyone |
| Categories | `POST /api/events/{id}/categories`, `PUT` and `DELETE /api/categories/{id}` | Organiser (owner) |
| Enrolments | `POST /api/categories/{id}/enrolments`, `GET /api/enrolments/my` | Participant |
| Enrolments | `GET /api/events/{id}/enrolments` | Organiser (owner) |
| Results | `GET /api/results/my` | Participant |
| Results | `GET /api/results/{id}` | Everyone |
| Results | `POST /api/results`, `PUT /api/results/{id}` | Organiser (owner) |

## Differences from the Part 1 plan

- **Register** takes `firstName` and `lastName` (the ERD splits the name) plus an optional `phoneNumber`. A duplicate email returns `400`, as the plan lists.
- **Profile `PUT`** accepts first name, last name, email and phone number.
- **Logout** returns `200`, but a JWT is stateless, so the client deletes its own token. There is no server-side session to close.
- **Delete event / delete category** return `409 Conflict` when dependent rows exist (categories, or enrolments), because the database uses no cascading deletes.
- **Results** use `overallPosition` and `categoryPosition` (as in the ERD) instead of a single `Position`. Extra failure codes (`400`, `403`, `404`, `409`) were added for invalid, foreign or duplicate results.
- **Enrolment** also returns `400` when the event is not open or the category is full.
- **Weather** uses the free Open-Meteo service (no API key needed) instead of a keyed provider.
- **Distance and event type** are stored on the category, as in the ERD, not on the event.
- The `GET` endpoints for events, categories, routes, weather and single results are open to everyone, which includes any logged-in role.
- There is no endpoint to create routes (the plan only lists `GET`), so routes are added directly in the database.
- **Database naming:** Entity Framework Core maps `DateTime` to `datetime2` (the SQL script used `DATETIME`) and generates its own names for keys, indexes and `CHECK` constraints. The rules themselves are the same as in the SQL script.

## CI/CD

`.github/workflows/dotnet.yml` builds the API and runs all tests on every push. `validate.yml` checks that the Part 1 files exist.

**Successful build screenshot:**

*[Paste your green CI/CD build screenshot here]*

