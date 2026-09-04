# RaceDay

A full-stack, cloud-aware, API-driven event management platform for South Africa's road running, walking, and cycling community. RaceDay lets Event Organisers create and manage events, categories, and participant results, while Participants can browse upcoming events, enter events, track their personal performance history, and prepare for race day using live weather and route information.

This repository currently contains **Part 1: System Planning and Database** — the ERD, the API endpoint plan, and the SQL database script. No application code has been written yet; Part 1 is a planning and design phase only.

## Roles

**Organiser**
Can create, edit, and delete events; manage event categories; view all participant enrolments for their events; and capture participant results after an event concludes.

**Participant**
Can register for an account, browse upcoming events, enter an event by selecting a category, view their own enrolments, track their personal race result history, and access route and live weather information to prepare for race day.

## Repository Structure

```
RaceDay
│
├── docs
│   ├── ERD.pdf
│   ├── APIEndpointPlan.pdf
│   └── RaceDayDatabase.sql
│
├── README.md
│
└── .github
    └── workflows
         └── validate.yml
```

## Part 1 Deliverables

- **ERD.pdf** — Entity Relationship Diagram for all six entities (User, Event, Category, Route, Enrolment, Result), showing primary keys, foreign keys, and cardinality for every relationship.
- **APIEndpointPlan.pdf** — Full endpoint specification table, grouped by resource (Authentication, Profile, Event, Category, Enrolment, Result, Location & Weather), to be implemented in Part 2.
- **RaceDayDatabase.sql** — SQL Server script that creates the full schema (matching the ERD exactly) and seeds it with sample data: 2 Organisers, 2 Participants, 3 Events, multiple Categories, Routes, and Enrolments.

## CI/CD

A GitHub Actions workflow (`.github/workflows/validate.yml`) runs on every push and checks that the required Part 1 files exist in the `docs` folder.

**Successful build screenshot:**

*[<img width="1162" height="212" alt="image" src="https://github.com/user-attachments/assets/d1cba342-2955-47e7-9676-0c9ed965c9b3" />
]*

## Video Presentation

**Unlisted YouTube link:** *Find the YouTube Link in Planning and Design Document*

The video covers: the ERD and its relationships, the API endpoint plan, the SQL script, and running the SQL script in SSMS.

## Figma Prototype 
Link: https://www.figma.com/make/NGOe7Hd3sYIWjFcENYOtfL/RaceDay-Booking-Platform-Design?fullscreen=1&t=jveMVQcC4cWN5YWS-1&code-node-id=0-6

## Notes

- `PasswordHash` values in the seed data are placeholders, not real hashes — password hashing will be implemented as part of the registration endpoint in Part 2.
- No AI-generated voiceover was used in the video presentation.
