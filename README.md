# RaceDay

A full-stack, API-driven event management platform for South Africa's road running, walking and cycling community. Event Organisers create and manage events, categories and results. Participants browse upcoming events, enter a category, see their race history, and prepare for race day with live weather, route information and directions.

| Part | What it is | Folder |
|---|---|---|
| 1 | Planning and database: ERD, endpoint plan, SQL script | `docs` |
| 2 | RESTful API (ASP.NET Core, EF Core, JWT, Swagger, unit tests) | `Part2` |
| 3 | MVC website, Azure Blob Storage image uploads, Docker | `Part3` |

## The two roles

**Organiser:** creates, edits and deletes their own events (with a banner image); adds, edits and deletes categories; sees every enrolment for their events and confirms them; captures and publishes finish times and positions; has a dashboard with enrolments per event and upcoming dates.

**Participant:** registers and logs in; browses and filters upcoming events; sees full event information, categories, route details, live weather and a directions link; enters an event by choosing a category; sees their enrolments and status (green Confirmed, amber Pending); sees their race history (for example "47th of 312 finishers"); updates their profile and uploads a profile picture.

Roles are enforced **inside the API** (`401` without a token, `403` for the wrong role or for another Organiser's event). The website also hides and protects pages by role, and redirects logged-out visitors to the login page.

## How the pieces fit

```
Browser  ->  RaceDay.Web (MVC, Docker)  ->  RaceDay.API  ->  SQL Server
                                                |
                                                +-------->  Azure Blob Storage (images)
```

The MVC website has **no database access and no business logic**. It only calls the API. Image files are sent to the API, and only the API talks to Azure Blob Storage.

## Repository structure

```
RaceDay
├── docs                        Part 1: ERD.pdf, APIEndpointPlan.pdf, RaceDayDatabase.sql
├── Part2
│   └── RaceDay.API             solution: RaceDay.API (the API) and RaceDay.Tests (xUnit tests)
├── Part3
│   └── RaceDay.Web             the MVC website, Dockerfile, compose.yaml
├── .github/workflows           validate.yml (Part 1 files), dotnet.yml (API, tests and website build)
└── README.md
```

## Setup and run

**You need:** the .NET 9 SDK, SQL Server (Express is fine), Visual Studio 2022 or the `dotnet` command line, Docker Desktop, and an Azure Storage account for images.

### 1. The API (`Part2/RaceDay.API/RaceDay.API`)

Set the private settings with user secrets, so no password or key is stored in GitHub:

```
dotnet user-secrets set "Jwt:Key" "any-random-text-of-at-least-32-characters"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=YOUR-SERVER\SQLEXPRESS;Database=RaceDayDataBase;Trusted_Connection=True;TrustServerCertificate=True;"
dotnet user-secrets set "AzureStorage:ConnectionString" "your Azure storage account connection string"
```

Create a **container** named `raceday-images` in the storage account with anonymous access level **Blob**, so the browser can show the uploaded images.

Run the API with the **`http`** launch profile (it listens on port 5012 for all network interfaces, which lets the Docker container reach it):

```
dotnet run --launch-profile http
```

In Development the database is created or upgraded automatically from the EF Core migrations. Swagger is at `http://localhost:5012/swagger`.

### 2. The website

Locally:

```
cd Part3/RaceDay.Web
dotnet run
```

The website reads the API address from `ApiSettings:BaseUrl` (default `http://localhost:5012`).

### 3. The website in Docker (one command)

Start the API first (step 1), then:

```
cd Part3/RaceDay.Web
docker compose up --build
```

Open **http://localhost:8080**. The container reaches the API on your computer through `host.docker.internal`. The same thing without Compose:

```
docker build -t raceday-web .
docker run --rm -p 8080:8080 -e ApiSettings__BaseUrl=http://host.docker.internal:5012 --name raceday-web raceday-web
```

If the container cannot reach the API, allow the API through the Windows Firewall when Windows asks (private networks) and check that the API is running with the `http` profile.

### Tests

```
cd Part2/RaceDay.API
dotnet test
```

## Login and sessions

The website logs in through `POST /api/auth/login`. The API returns a signed JWT. The website keeps the token **in the server-side ASP.NET Core session** (never in the page) and puts the user's role in a login cookie. Every call to the API sends the token. Logging out calls the API, signs out the cookie and clears the session. If the session ends, the user is sent to the login page.

## Differences from the plans

**Part 2 (from the Part 1 plan):** register takes first and last name plus an optional phone number; a duplicate email returns `400`; logout is stateless (the client discards the token); deleting an event or category returns `409` while dependent rows exist; results use `overallPosition` and `categoryPosition`; weather uses the free Open-Meteo service; distance and event type live on the category; routes are added directly in the database.

**Part 3 additions to the API:**
- `POST /api/events/{id}/banner` and `POST /api/profile/picture` upload images to Azure Blob Storage.
- `GET /api/events/mine` returns an Organiser's events with enrolment counts (for the dashboard).
- `PUT /api/enrolments/{id}/payment` lets an Organiser confirm an enrolment.
- `GET /api/events/{id}/results` lists the results of an event.
- Results now include the number of finishers, so the website can show "47th of 312 finishers".
- Two new database columns: `Event.BannerImageUrl` and `User.ProfilePictureUrl` (migration `AddImageUrls`).
- The database stores `Pending` and `Paid`; the website shows `Paid` as **Confirmed**.
- Entity Framework maps `DateTime` to `datetime2` and generates its own constraint and index names.

## CI/CD

`.github/workflows/dotnet.yml` builds the API, runs all unit tests and builds the MVC website on every push.

**Successful build screenshot:**

*[<img width="1152" height="572" alt="image" src="https://github.com/user-attachments/assets/ff3fc3a2-8ebf-4acf-baa3-a25f606e9596" />
]*

## AI use disclosure

* I used Claude (Anthropic) to help plan the project, explain concepts, and debug code. I reviewed, ran and tested the code, and I can explain how it works. 
