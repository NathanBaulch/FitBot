DROP TABLE IF EXISTS [User];

DROP TABLE IF EXISTS [Workout];

DROP TABLE IF EXISTS [Activity];

DROP TABLE IF EXISTS [Set];

DROP TABLE IF EXISTS [Achievement];

DROP VIEW IF EXISTS [WorkoutView];

DROP VIEW IF EXISTS [ActivityView];

DROP VIEW IF EXISTS [SetView];

DROP VIEW IF EXISTS [AchievementView];

CREATE TABLE [User] (
  [Id] [bigint] PRIMARY KEY NOT NULL,
  [Username] [nvarchar] (100) NOT NULL,
  [InsertDate] [datetime2] NOT NULL,
  [UpdateDate] [datetime2],
  UNIQUE([Username])
);

CREATE TABLE [Workout] (
  [Id] [bigint] PRIMARY KEY NOT NULL,
  [UserId] [bigint] NOT NULL,
  [Date] [datetime2] NULL,
  [Points] [int] NULL,
  [InsertDate] [datetime2] NOT NULL,
  [UpdateDate] [datetime2],
  [ActivitiesHash] [int] NOT NULL,
  FOREIGN KEY ([UserId]) REFERENCES [User] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_Workout_UserId] ON [Workout] ([UserId]);

CREATE TABLE [Activity] (
  [Id] INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
  [WorkoutId] [bigint] NOT NULL,
  [Sequence] [int] NOT NULL,
  [Name] [nvarchar] (100) NOT NULL,
  [Group] [nvarchar] (100) NULL,
  [Note] [text] NULL,
  UNIQUE ([WorkoutId], [Sequence]),
  FOREIGN KEY ([WorkoutId]) REFERENCES [Workout] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_Activity_WorkoutId] ON [Activity] ([WorkoutId]);

CREATE TABLE [Set] (
  [Id] INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
  [ActivityId] [bigint] NOT NULL,
  [Sequence] [int] NOT NULL,
  [Points] [int] NULL,
  [Distance] REAL NULL,
  [Duration] REAL NULL,
  [Speed] REAL NULL,
  [Repetitions] REAL NULL,
  [Weight] REAL NULL,
  [HeartRate] REAL NULL,
  [Incline] REAL NULL,
  [Difficulty] [nvarchar] (100) NULL,
  [IsImperial] [bit] NOT NULL,
  [IsPr] [bit] NOT NULL,
  UNIQUE ([ActivityId], [Sequence]),
  FOREIGN KEY ([ActivityId]) REFERENCES [Activity] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_Set_ActivityId] ON [Set] ([ActivityId]);

CREATE TABLE [Achievement] (
  [Id] INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
  [WorkoutId] [bigint] NOT NULL,
  [Type] [nvarchar] (100) NOT NULL,
  [Group] [nvarchar] (100) NULL,
  [Activity] [nvarchar] (100) NULL,
  [Distance] REAL NULL,
  [Duration] REAL NULL,
  [Speed] REAL NULL,
  [Repetitions] REAL NULL,
  [Weight] REAL NULL,
  [CommentId] [bigint] NULL,
  [CommentText] [text] NULL,
  [IsPropped] [bit] NOT NULL,
  [InsertDate] [datetime2] NOT NULL,
  [UpdateDate] [datetime2],
  FOREIGN KEY ([WorkoutId]) REFERENCES [Workout] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_Achievement_WorkoutId] ON [Achievement] ([WorkoutId]);

CREATE VIEW [WorkoutView] AS
SELECT u.[Id] [UserId], u.[Username], u.[InsertDate] [UserInsertDate],
       w.[Id] [WorkoutId], w.[Date], w.[Points], w.[InsertDate] [WorkoutInsertDate], w.[ActivitiesHash]
FROM [User] u, [Workout] w
WHERE u.[Id] = w.[UserId];

CREATE VIEW [ActivityView] AS
SELECT u.[Id] [UserId], u.[Username], u.[InsertDate] [UserInsertDate],
       w.[Id] [WorkoutId], w.[Date], w.[Points], w.[InsertDate] [WorkoutInsertDate], w.[ActivitiesHash],
       a.[Id] [ActivityId], a.[Sequence], a.[Name], a.[Group], a.[Note]
FROM [User] u, [Workout] w, [Activity] a
WHERE u.[Id] = w.[UserId]
  AND w.[Id] = a.[WorkoutId];

CREATE VIEW [SetView] AS
SELECT u.[Id] [UserId], u.[Username], u.[InsertDate] [UserInsertDate],
       w.[Id] [WorkoutId], w.[Date], w.[Points] [WorkoutPoints], w.[InsertDate] [WorkoutInsertDate], w.[ActivitiesHash],
       a.[Id] [ActivityId], a.[Sequence] [ActivitySequence], a.[Name], a.[Group], a.[Note],
       s.[Id] [SetId], s.[Sequence] [SetSequence], s.[Points] [SetPoints],
       s.[Distance], s.[Duration], s.[Speed], s.[Repetitions], s.[Weight],
       s.[HeartRate], s.[Incline], s.[Difficulty], s.[IsImperial], s.[IsPr]
FROM [User] u, [Workout] w, [Activity] a, [Set] s
WHERE u.[Id] = w.[UserId]
  AND w.[Id] = a.[WorkoutId]
  AND a.[Id] = s.[ActivityId];

CREATE VIEW [AchievementView] AS
SELECT u.[Id] [UserId], u.[Username], u.[InsertDate] [UserInsertDate],
       w.[Id] [WorkoutId], w.[Date], w.[Points], w.[InsertDate] [WorkoutInsertDate], w.[ActivitiesHash],
       a.[Id] [AchievementId], a.[Type], a.[Group], a.[Activity],
       a.[Distance], a.[Duration], a.[Speed], a.[Repetitions], a.[Weight],
       a.[CommentId], a.[CommentText], a.[IsPropped], a.[InsertDate] [AchievementInsertDate]
FROM [User] u, [Workout] w, [Achievement] a
WHERE u.[Id] = w.[UserId]
  AND w.[Id] = a.[WorkoutId];
