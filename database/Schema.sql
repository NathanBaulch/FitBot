CREATE DATABASE [FitBot]
GO

USE [FitBot]
GO

CREATE TABLE [dbo].[User](
	[Id] [bigint] PRIMARY KEY NOT NULL,
	[Username] [nvarchar](50) NOT NULL
)
GO

CREATE TABLE [dbo].[Workout](
	[Id] [bigint] PRIMARY KEY NOT NULL,
	[UserId] [bigint] NOT NULL,
	[Date] [datetime2] NULL,
	[Points] [int] NULL,
	[ImportDate] [datetime2] NOT NULL,
	[Hash] [int] NOT NULL
)
GO

CREATE INDEX [IX_Workout_UserId] ON [dbo].[Workout]([UserId])
GO

ALTER TABLE [dbo].[Workout] ADD CONSTRAINT [FK_User_Workout]
FOREIGN KEY ([UserId]) REFERENCES [dbo].[User]([Id]) ON DELETE CASCADE
GO

CREATE TABLE [dbo].[Activity](
	[Id] [bigint] PRIMARY KEY IDENTITY NOT NULL,
	[WorkoutId] [bigint] NOT NULL,
	[Sequence] [int] NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
	[Note] [text] NULL
)
GO

CREATE INDEX [IX_Activity_WorkoutId] ON [dbo].[Activity]([WorkoutId])
GO

ALTER TABLE [dbo].[Activity] ADD CONSTRAINT [FK_Workout_Activity]
FOREIGN KEY ([WorkoutId]) REFERENCES [dbo].[Workout]([Id]) ON DELETE CASCADE
GO

CREATE TABLE [dbo].[Set](
	[Id] [bigint] PRIMARY KEY IDENTITY NOT NULL,
	[ActivityId] [bigint] NOT NULL,
	[Sequence] [int] NOT NULL,
	[Points] [int] NULL,
	[Distance] [float] NULL,
	[Duration] [int] NULL,
	[Speed] [float] NULL,
	[Repetitions] [int] NULL,
	[Weight] [float] NULL,
	[HeartRate] [float] NULL,
	[Incline] [float] NULL,
	[Difficulty] [nvarchar](50) NULL,
	[IsPb] [bit] NOT NULL
)
GO

CREATE INDEX [IX_Set_ActivityId] ON [dbo].[Set]([ActivityId])
GO

ALTER TABLE [dbo].[Set] ADD CONSTRAINT [FK_Activity_Set]
FOREIGN KEY ([ActivityId]) REFERENCES [dbo].[Activity]([Id]) ON DELETE CASCADE
GO
