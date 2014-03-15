USE [master]
GO

DROP DATABASE [FitBot]
GO

CREATE DATABASE [FitBot]
GO

USE [FitBot]
GO

CREATE TABLE [User] (
	[Id] [bigint] PRIMARY KEY NOT NULL,
	[Username] [nvarchar] (100) NOT NULL,
	[DirtyDate] [datetime2] NULL,
	UNIQUE([Username])
)
GO

CREATE TABLE [Workout] (
	[Id] [bigint] PRIMARY KEY NOT NULL,
	[UserId] [bigint] NOT NULL,
	[Date] [datetime2] NULL,
	[Points] [int] NULL,
	[CommentId] [bigint] NULL,
	[CommentHash] [int] NULL,
	[IsPropped] [bit] NOT NULL,
	[SyncDate] [datetime2] NOT NULL,
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
	[Distance] [decimal](9, 2) NULL,
	[Duration] [decimal](9, 2) NULL,
	[Speed] [decimal](9, 2) NULL,
	[Repetitions] [decimal](9, 2) NULL,
	[Weight] [decimal](9, 2) NULL,
	[HeartRate] [decimal](9, 2) NULL,
	[Incline] [decimal](9, 2) NULL,
	[Difficulty] [nvarchar] (100) NULL,
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
	[Group] [nvarchar] (100) NOT NULL,
	[Distance] [decimal](9, 2) NULL,
	[Duration] [decimal](9, 2) NULL,
	[Speed] [decimal](9, 2) NULL,
	[Repetitions] [decimal](9, 2) NULL,
	[Weight] [decimal](9, 2) NULL,
	UNIQUE ([WorkoutId], [Type], [Group]),
	FOREIGN KEY ([WorkoutId]) REFERENCES [Workout] ([Id]) ON DELETE CASCADE
)
GO

CREATE INDEX [IX_Achievement_WorkoutId] ON [Achievement] ([WorkoutId])
GO
