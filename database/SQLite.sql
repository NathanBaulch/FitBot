DROP TABLE IF EXISTS [User];

DROP TABLE IF EXISTS [Workout];

DROP TABLE IF EXISTS [Activity];

DROP TABLE IF EXISTS [Set];

DROP TABLE IF EXISTS [Achievement];

DROP VIEW IF EXISTS [WorkoutView];

DROP VIEW IF EXISTS [ActivityView];

DROP VIEW IF EXISTS [SetView];

DROP VIEW IF EXISTS [AchievementView];

CREATE TABLE [User]
(
    [Id]         BIGINT        NOT NULL PRIMARY KEY,
    [Username]   NVARCHAR(100) NOT NULL UNIQUE,
    [InsertDate] DATETIME      NOT NULL,
    [UpdateDate] DATETIME
);

CREATE TABLE [Workout]
(
    [Id]             BIGINT   NOT NULL PRIMARY KEY,
    [UserId]         BIGINT   NOT NULL REFERENCES [User] ON DELETE CASCADE,
    [Date]           DATETIME,
    [Points]         INT,
    [InsertDate]     DATETIME NOT NULL,
    [UpdateDate]     DATETIME,
    [ActivitiesHash] INT      NOT NULL
);

CREATE INDEX [IX_Workout_UserId] ON [Workout] ([UserId]);

CREATE TABLE [Activity]
(
    [Id]        INTEGER       NOT NULL PRIMARY KEY AUTOINCREMENT,
    [WorkoutId] BIGINT        NOT NULL REFERENCES [Workout] ON DELETE CASCADE,
    [Sequence]  INT           NOT NULL,
    [Name]      NVARCHAR(100) NOT NULL,
    [Group]     NVARCHAR(100),
    [Note]      TEXT,
    UNIQUE ([WorkoutId], [Sequence])
);

CREATE INDEX [IX_Activity_WorkoutId] ON [Activity] ([WorkoutId]);

CREATE TABLE [Set]
(
    [Id]          INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    [ActivityId]  BIGINT  NOT NULL REFERENCES [Activity] ON DELETE CASCADE,
    [Sequence]    INT     NOT NULL,
    [Points]      INT,
    [Distance]    REAL,
    [Duration]    REAL,
    [Speed]       REAL,
    [Repetitions] REAL,
    [Weight]      REAL,
    [HeartRate]   REAL,
    [Difficulty]  NVARCHAR(100),
    [IsImperial]  BOOLEAN NOT NULL,
    [IsPr]        BOOLEAN NOT NULL,
    UNIQUE ([ActivityId], [Sequence])
);

CREATE INDEX [IX_Set_ActivityId] ON [Set] ([ActivityId]);

CREATE TABLE [Achievement]
(
    [Id]          INTEGER       NOT NULL PRIMARY KEY AUTOINCREMENT,
    [WorkoutId]   BIGINT        NOT NULL REFERENCES [Workout] ON DELETE CASCADE,
    [Type]        NVARCHAR(100) NOT NULL,
    [Group]       NVARCHAR(100),
    [Activity]    NVARCHAR(100),
    [Distance]    REAL,
    [Duration]    REAL,
    [Speed]       REAL,
    [Repetitions] REAL,
    [Weight]      REAL,
    [CommentId]   BIGINT,
    [CommentText] TEXT,
    [IsPropped]   BOOLEAN       NOT NULL,
    [InsertDate]  DATETIME      NOT NULL,
    [UpdateDate]  DATETIME
);

CREATE INDEX [IX_Achievement_WorkoutId] ON [Achievement] ([WorkoutId]);

CREATE VIEW [WorkoutView] AS
SELECT u.[Id]         [UserId],
       u.[Username],
       u.[InsertDate] [UserInsertDate],
       w.[Id]         [WorkoutId],
       w.[Date],
       w.[Points],
       w.[InsertDate] [WorkoutInsertDate],
       w.[ActivitiesHash]
FROM [User] u,
     [Workout] w
WHERE u.[Id] = w.[UserId];

CREATE VIEW [ActivityView] AS
SELECT u.[Id]         [UserId],
       u.[Username],
       u.[InsertDate] [UserInsertDate],
       w.[Id]         [WorkoutId],
       w.[Date],
       w.[Points],
       w.[InsertDate] [WorkoutInsertDate],
       w.[ActivitiesHash],
       a.[Id]         [ActivityId],
       a.[Sequence],
       a.[Name],
       a.[Group],
       a.[Note]
FROM [User] u,
     [Workout] w,
     [Activity] a
WHERE u.[Id] = w.[UserId]
  AND w.[Id] = a.[WorkoutId];

CREATE VIEW [SetView] AS
SELECT u.[Id]         [UserId],
       u.[Username],
       u.[InsertDate] [UserInsertDate],
       w.[Id]         [WorkoutId],
       w.[Date],
       w.[Points]     [WorkoutPoints],
       w.[InsertDate] [WorkoutInsertDate],
       w.[ActivitiesHash],
       a.[Id]         [ActivityId],
       a.[Sequence]   [ActivitySequence],
       a.[Name],
       a.[Group],
       a.[Note],
       s.[Id]         [SetId],
       s.[Sequence]   [SetSequence],
       s.[Points]     [SetPoints],
       s.[Distance],
       s.[Duration],
       s.[Speed],
       s.[Repetitions],
       s.[Weight],
       s.[HeartRate],
       s.[Difficulty],
       s.[IsImperial],
       s.[IsPr]
FROM [User] u,
     [Workout] w,
     [Activity] a,
     [Set] s
WHERE u.[Id] = w.[UserId]
  AND w.[Id] = a.[WorkoutId]
  AND a.[Id] = s.[ActivityId];

CREATE VIEW [AchievementView] AS
SELECT u.[Id]         [UserId],
       u.[Username],
       u.[InsertDate] [UserInsertDate],
       w.[Id]         [WorkoutId],
       w.[Date],
       w.[Points],
       w.[InsertDate] [WorkoutInsertDate],
       w.[ActivitiesHash],
       a.[Id]         [AchievementId],
       a.[Type],
       a.[Group],
       a.[Activity],
       a.[Distance],
       a.[Duration],
       a.[Speed],
       a.[Repetitions],
       a.[Weight],
       a.[CommentId],
       a.[CommentText],
       a.[IsPropped],
       a.[InsertDate] [AchievementInsertDate]
FROM [User] u,
     [Workout] w,
     [Achievement] a
WHERE u.[Id] = w.[UserId]
  AND w.[Id] = a.[WorkoutId];
