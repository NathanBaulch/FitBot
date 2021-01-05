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

CREATE TABLE [User]
(
    [Id]         BIGINT        NOT NULL PRIMARY KEY,
    [Username]   NVARCHAR(100) NOT NULL UNIQUE,
    [InsertDate] DATETIME2     NOT NULL,
    [UpdateDate] DATETIME2
)
GO

CREATE TABLE [Workout]
(
    [Id]             BIGINT    NOT NULL PRIMARY KEY,
    [UserId]         BIGINT    NOT NULL REFERENCES [User] ON DELETE CASCADE,
    [Date]           DATETIME2,
    [Points]         INT,
    [ActivitiesHash] INT       NOT NULL,
    [InsertDate]     DATETIME2 NOT NULL,
    [UpdateDate]     DATETIME2
)
GO

CREATE INDEX [IX_Workout_UserId] ON [Workout] ([UserId])
GO

CREATE TABLE [Activity]
(
    [Id]        BIGINT        NOT NULL PRIMARY KEY IDENTITY,
    [WorkoutId] BIGINT        NOT NULL REFERENCES [Workout] ON DELETE CASCADE,
    [Sequence]  INT           NOT NULL,
    [Name]      NVARCHAR(100) NOT NULL,
    [Group]     NVARCHAR(100),
    [Note]      TEXT,
    UNIQUE ([WorkoutId], [Sequence])
)
GO

CREATE INDEX [IX_Activity_WorkoutId] ON [Activity] ([WorkoutId])
GO

CREATE TABLE [Set]
(
    [Id]          BIGINT NOT NULL PRIMARY KEY IDENTITY,
    [ActivityId]  BIGINT NOT NULL REFERENCES [Activity] ON DELETE CASCADE,
    [Sequence]    INT    NOT NULL,
    [Points]      INT,
    [Distance]    DECIMAL(19, 2),
    [Duration]    DECIMAL(9, 1),
    [Speed]       DECIMAL(9, 2),
    [Repetitions] DECIMAL(9, 2),
    [Weight]      DECIMAL(9, 2),
    [HeartRate]   DECIMAL(9, 2),
    [Difficulty]  NVARCHAR(100),
    [IsImperial]  BIT    NOT NULL,
    [IsPr]        BIT    NOT NULL,
    UNIQUE ([ActivityId], [Sequence])
)
GO

CREATE INDEX [IX_Set_ActivityId] ON [Set] ([ActivityId])
GO

CREATE TABLE [Achievement]
(
    [Id]          BIGINT        NOT NULL PRIMARY KEY IDENTITY,
    [WorkoutId]   BIGINT        NOT NULL REFERENCES [Workout] ON DELETE CASCADE,
    [Type]        NVARCHAR(100) NOT NULL,
    [Group]       NVARCHAR(100),
    [Activity]    NVARCHAR(100),
    [Distance]    DECIMAL(19, 2),
    [Duration]    DECIMAL(9, 2),
    [Speed]       DECIMAL(9, 2),
    [Repetitions] DECIMAL(9, 2),
    [Weight]      DECIMAL(9, 2),
    [CommentId]   BIGINT,
    [CommentText] TEXT,
    [IsPropped]   BIT           NOT NULL,
    [InsertDate]  DATETIME2     NOT NULL,
    [UpdateDate]  DATETIME2
)
GO

CREATE INDEX [IX_Achievement_WorkoutId] ON [Achievement] ([WorkoutId])
GO

CREATE VIEW [WorkoutView] AS
SELECT u.[Id]         [UserId],
       u.[Username],
       u.[InsertDate] [UserInsertDate],
       w.[Id]         [WorkoutId],
       w.[Date],
       w.[Points],
       w.[ActivitiesHash],
       w.[InsertDate] [WorkoutInsertDate]
FROM [User] u,
     [Workout] w
WHERE u.[Id] = w.[UserId]
GO

CREATE VIEW [ActivityView] AS
SELECT u.[Id]         [UserId],
       u.[Username],
       u.[InsertDate] [UserInsertDate],
       w.[Id]         [WorkoutId],
       w.[Date],
       w.[Points],
       w.[ActivitiesHash],
       w.[InsertDate] [WorkoutInsertDate],
       a.[Id]         [ActivityId],
       a.[Sequence],
       a.[Name],
       a.[Group],
       a.[Note]
FROM [User] u,
     [Workout] w,
     [Activity] a
WHERE u.[Id] = w.[UserId]
  AND w.[Id] = a.[WorkoutId]
GO

CREATE VIEW [SetView] AS
SELECT u.[Id]         [UserId],
       u.[Username],
       u.[InsertDate] [UserInsertDate],
       w.[Id]         [WorkoutId],
       w.[Date],
       w.[Points]     [WorkoutPoints],
       w.[ActivitiesHash],
       w.[InsertDate] [WorkoutInsertDate],
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
  AND a.[Id] = s.[ActivityId]
GO

CREATE VIEW [AchievementView] AS
SELECT u.[Id]         [UserId],
       u.[Username],
       u.[InsertDate] [UserInsertDate],
       w.[Id]         [WorkoutId],
       w.[Date],
       w.[Points],
       w.[ActivitiesHash],
       w.[InsertDate] [WorkoutInsertDate],
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
  AND w.[Id] = a.[WorkoutId]
GO
