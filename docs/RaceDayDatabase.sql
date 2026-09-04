CREATE DATABASE RaceDayDB2;
GO
USE RaceDayDB2;
GO

-- ============================================================
-- RaceDay Database Script
-- Part 1, Section C - matches the Part 1 ERD exactly
-- Run against a clean SQL Server instance in SSMS
-- ============================================================

-- ============================================================
-- 1. CREATE TABLES
-- ============================================================

CREATE TABLE [User] (
    UserID          INT IDENTITY(1,1) PRIMARY KEY,
    FirstName       NVARCHAR(50)  NOT NULL,
    LastName        NVARCHAR(50)  NOT NULL,
    Email           NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash    NVARCHAR(255) NOT NULL,
    Role            NVARCHAR(20)  NOT NULL CHECK (Role IN ('Organiser', 'Participant')),
    PhoneNumber     NVARCHAR(20)  NULL,
    CreatedAt       DATETIME      NOT NULL DEFAULT GETDATE()
);

CREATE TABLE Event (
    EventID         INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserID     INT NOT NULL,
    EventName       NVARCHAR(100) NOT NULL,
    EventDate       DATE NOT NULL,
    Location        NVARCHAR(150) NOT NULL,
    Latitude        DECIMAL(9,6) NULL,
    Longitude       DECIMAL(9,6) NULL,
    Province        NVARCHAR(50) NOT NULL,
    Description     NVARCHAR(1000) NULL,
    Status          NVARCHAR(20) NOT NULL DEFAULT 'Upcoming' CHECK (Status IN ('Upcoming', 'Completed', 'Cancelled')),
    CONSTRAINT FK_Event_Organiser FOREIGN KEY (OrganiserID) REFERENCES [User](UserID)
);

CREATE TABLE Category (
    CategoryID      INT IDENTITY(1,1) PRIMARY KEY,
    EventID         INT NOT NULL,
    CategoryName    NVARCHAR(100) NOT NULL,
    DistanceKM      DECIMAL(5,2) NOT NULL,
    EventType       NVARCHAR(20) NOT NULL CHECK (EventType IN ('run', 'walk', 'cycle')),
    EntryFee        DECIMAL(8,2) NOT NULL DEFAULT 0,
    MaxParticipants INT NOT NULL,
    CONSTRAINT FK_Category_Event FOREIGN KEY (EventID) REFERENCES Event(EventID)
);

CREATE TABLE Route (
    RouteID         INT IDENTITY(1,1) PRIMARY KEY,
    CategoryID      INT NOT NULL UNIQUE,
    RouteName       NVARCHAR(100) NOT NULL,
    ElevationGainM  DECIMAL(6,1) NULL,
    StartPoint      NVARCHAR(150) NOT NULL,
    EndPoint        NVARCHAR(150) NOT NULL,
    MapURL          NVARCHAR(255) NULL,
    CONSTRAINT FK_Route_Category FOREIGN KEY (CategoryID) REFERENCES Category(CategoryID)
);

CREATE TABLE Enrolment (
    EnrolmentID     INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantID   INT NOT NULL,
    CategoryID      INT NOT NULL,
    EnrolmentDate   DATETIME NOT NULL DEFAULT GETDATE(),
    RaceNumber      NVARCHAR(20) NOT NULL,
    PaymentStatus   NVARCHAR(20) NOT NULL DEFAULT 'Pending' CHECK (PaymentStatus IN ('Pending', 'Paid')),
    CONSTRAINT FK_Enrolment_Participant FOREIGN KEY (ParticipantID) REFERENCES [User](UserID),
    CONSTRAINT FK_Enrolment_Category FOREIGN KEY (CategoryID) REFERENCES Category(CategoryID),
    CONSTRAINT UQ_Enrolment_ParticipantCategory UNIQUE (ParticipantID, CategoryID)
);

CREATE TABLE Result (
    ResultID           INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentID        INT NOT NULL UNIQUE,
    FinishTime         TIME NULL,
    OverallPosition     INT NULL,
    CategoryPosition   INT NULL,
    Status             NVARCHAR(20) NOT NULL DEFAULT 'Finished' CHECK (Status IN ('Finished', 'DNF', 'DSQ')),
    CONSTRAINT FK_Result_Enrolment FOREIGN KEY (EnrolmentID) REFERENCES Enrolment(EnrolmentID)
);


-- ============================================================
-- 2. SEED DATA
-- ============================================================

-- Users: 2 Organisers, 2 Participants
-- Note: PasswordHash values below are placeholders for sample data only,
-- not real bcrypt/hash output. Part 2's API will hash real passwords properly.

INSERT INTO [User] (FirstName, LastName, Email, PasswordHash, Role, PhoneNumber)
VALUES
('Thabo', 'Nkosi', 'thabo.nkosi@raceday.co.za', 'HASHED_PASSWORD_1', 'Organiser', '0821234567'),
('Anele', 'Mokoena', 'anele.mokoena@raceday.co.za', 'HASHED_PASSWORD_2', 'Organiser', '0839876543'),
('Lindiwe', 'Dlamini', 'lindiwe.d@example.co.za', 'HASHED_PASSWORD_3', 'Participant', '0712345678'),
('Sipho', 'Mahlangu', 'sipho.m@example.co.za', 'HASHED_PASSWORD_4', 'Participant', '0765554321');

-- Events: 3 events, each organised by one of the two Organisers

INSERT INTO Event (OrganiserID, EventName, EventDate, Location, Latitude, Longitude, Province, Description, Status)
VALUES
(1, 'Soweto Marathon', '2026-11-14', 'Chris Hani Baragwanath, Soweto', -26.259700, 27.938500, 'Gauteng', 'Annual community road running event through the streets of Soweto.', 'Upcoming'),
(1, 'Diepkloof Community Walk', '2026-11-21', 'Diepkloof Community Hall, Soweto', -26.259800, 27.951200, 'Gauteng', 'Free community fun walk supporting local charities.', 'Upcoming'),
(2, 'Cape Town Cycle Tour', '2027-03-08', 'Green Point, Cape Town', -33.906500, 18.410900, 'Western Cape', 'Iconic 109km cycling event around the Cape Peninsula.', 'Upcoming');

-- Categories: at least one per event, matching event type

INSERT INTO Category (EventID, CategoryName, DistanceKM, EventType, EntryFee, MaxParticipants)
VALUES
(1, '10km Run', 10.00, 'run', 120.00, 2500),
(1, '21km Half Marathon', 21.10, 'run', 280.00, 3000),
(1, '42km Marathon', 42.20, 'run', 350.00, 1500),
(2, '5km Fun Walk', 5.00, 'walk', 0.00, 800),
(3, '109km Cycle Tour', 109.00, 'cycle', 650.00, 16000);

-- Routes: one per category (1:1 with Category)

INSERT INTO Route (CategoryID, RouteName, ElevationGainM, StartPoint, EndPoint, MapURL)
VALUES
(1, 'Soweto 10km Loop', 85.0, 'Chris Hani Baragwanath', 'Elias Motsoaledi Square', 'https://maps.example.com/route1'),
(2, 'Soweto Half Marathon Route', 160.0, 'Chris Hani Baragwanath', 'Orlando Stadium', 'https://maps.example.com/route2'),
(3, 'Soweto Full Marathon Route', 310.0, 'Chris Hani Baragwanath', 'Orlando Stadium', 'https://maps.example.com/route3'),
(4, 'Diepkloof Community Walk Route', 20.0, 'Diepkloof Community Hall', 'Diepkloof Community Hall', 'https://maps.example.com/route4'),
(5, 'Cape Town Cycle Tour Route', 920.0, 'Green Point', 'Green Point', 'https://maps.example.com/route5');

-- Enrolments: sample entries for the 2 Participants across different events/categories

INSERT INTO Enrolment (ParticipantID, CategoryID, RaceNumber, PaymentStatus)
VALUES
(3, 2, '4021', 'Paid'),   -- Lindiwe entered Soweto 21km Half Marathon
(4, 1, '4022', 'Paid'),   -- Sipho entered Soweto 10km Run
(3, 4, '1003', 'Pending'),-- Lindiwe entered Diepkloof 5km Fun Walk
(4, 5, '7710', 'Paid');   -- Sipho entered Cape Town Cycle Tour

-- Results: sample captured result for a completed enrolment

INSERT INTO Result (EnrolmentID, FinishTime, OverallPosition, CategoryPosition, Status)
VALUES
(1, '01:52:10', 88, 14, 'Finished');
