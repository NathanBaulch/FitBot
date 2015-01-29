USE [master]
GO

DROP DATABASE [FitBot]
GO

CREATE DATABASE [FitBot]
GO

USE [FitBot]
GO

ALTER DATABASE [FitBot] SET ALLOW_SNAPSHOT_ISOLATION ON
GO

ALTER DATABASE [FitBot] SET READ_COMMITTED_SNAPSHOT ON
GO

CREATE TABLE [User] (
  [Id] [bigint] PRIMARY KEY NOT NULL,
  [Username] [nvarchar] (100) NOT NULL,
  [InsertDate] [datetime2] NOT NULL,
  UNIQUE([Username])
)
GO

CREATE TABLE [Workout] (
  [Id] [bigint] PRIMARY KEY NOT NULL,
  [UserId] [bigint] NOT NULL,
  [Date] [datetime2] NULL,
  [Points] [int] NULL,
  [InsertDate] [datetime2] NOT NULL,
  [ActivitiesHash] [int] NOT NULL,
  FOREIGN KEY ([UserId]) REFERENCES [User] ([Id]) ON DELETE CASCADE
)
GO

CREATE INDEX [IX_Workout_UserId] ON [Workout] ([UserId])
GO

CREATE TABLE [Activity] (
  [Id] [bigint] PRIMARY KEY IDENTITY NOT NULL,
  [WorkoutId] [bigint] NOT NULL,
  [Sequence] [int] NOT NULL,
  [Name] [nvarchar] (100) NOT NULL,
  [Group] [nvarchar] (100) NULL,
  [Note] [text] NULL,
  UNIQUE ([WorkoutId], [Sequence]),
  FOREIGN KEY ([WorkoutId]) REFERENCES [Workout] ([Id]) ON DELETE CASCADE
)
GO

CREATE INDEX [IX_Activity_WorkoutId] ON [Activity] ([WorkoutId])
GO

CREATE TABLE [Set] (
  [Id] [bigint] PRIMARY KEY IDENTITY NOT NULL,
  [ActivityId] [bigint] NOT NULL,
  [Sequence] [int] NOT NULL,
  [Points] [int] NULL,
  [Distance] [decimal](19, 2) NULL,
  [Duration] [decimal](9, 2) NULL,
  [Speed] [decimal](9, 2) NULL,
  [Repetitions] [decimal](9, 2) NULL,
  [Weight] [decimal](9, 2) NULL,
  [HeartRate] [decimal](9, 2) NULL,
  [Incline] [decimal](9, 2) NULL,
  [Difficulty] [nvarchar] (100) NULL,
  [IsImperial] [bit] NOT NULL,
  [IsPr] [bit] NOT NULL,
  UNIQUE ([ActivityId], [Sequence]),
  FOREIGN KEY ([ActivityId]) REFERENCES [Activity] ([Id]) ON DELETE CASCADE
)
GO

CREATE INDEX [IX_Set_ActivityId] ON [Set] ([ActivityId])
GO

CREATE TABLE [Achievement] (
  [Id] [bigint] PRIMARY KEY IDENTITY NOT NULL,
  [WorkoutId] [bigint] NOT NULL,
  [Type] [nvarchar] (100) NOT NULL,
  [Group] [nvarchar] (100) NULL,
  [Activity] [nvarchar] (100) NULL,
  [Distance] [decimal](19, 2) NULL,
  [Duration] [decimal](9, 2) NULL,
  [Speed] [decimal](9, 2) NULL,
  [Repetitions] [decimal](9, 2) NULL,
  [Weight] [decimal](9, 2) NULL,
  [CommentId] [bigint] NULL,
  [CommentText] [text] NULL,
  [IsPropped] [bit] NOT NULL,
  [InsertDate] [datetime2] NOT NULL,
  FOREIGN KEY ([WorkoutId]) REFERENCES [Workout] ([Id]) ON DELETE CASCADE
)
GO

CREATE INDEX [IX_Achievement_WorkoutId] ON [Achievement] ([WorkoutId])
GO

CREATE VIEW [WorkoutView] AS
SELECT u.[Id] [UserId], u.[Username], u.[InsertDate] [UserInsertDate],
       w.[Id] [WorkoutId], w.[Date], w.[Points], w.[InsertDate] [WorkoutInsertDate], w.[ActivitiesHash]
FROM [User] u, [Workout] w
WHERE u.[Id] = w.[UserId]
GO

CREATE VIEW [ActivityView] AS
SELECT u.[Id] [UserId], u.[Username], u.[InsertDate] [UserInsertDate],
       w.[Id] [WorkoutId], w.[Date], w.[Points], w.[InsertDate] [WorkoutInsertDate], w.[ActivitiesHash],
       a.[Id] [ActivityId], a.[Sequence], a.[Name], a.[Group], a.[Note]
FROM [User] u, [Workout] w, [Activity] a
WHERE u.[Id] = w.[UserId]
  AND w.[Id] = a.[WorkoutId]
GO

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
  AND a.[Id] = s.[ActivityId]
GO

CREATE VIEW [AchievementView] AS
SELECT u.[Id] [UserId], u.[Username], u.[InsertDate] [UserInsertDate],
       w.[Id] [WorkoutId], w.[Date], w.[Points], w.[InsertDate] [WorkoutInsertDate], w.[ActivitiesHash],
       a.[Id] [AchievementId], a.[Type], a.[Group], a.[Activity],
       a.[Distance], a.[Duration], a.[Speed], a.[Repetitions], a.[Weight],
       a.[CommentId], a.[CommentText], a.[IsPropped], a.[InsertDate] [AchievementInsertDate]
FROM [User] u, [Workout] w, [Achievement] a
WHERE u.[Id] = w.[UserId]
  AND w.[Id] = a.[WorkoutId]
GO
