DROP TABLE IF EXISTS [User];

DROP TABLE IF EXISTS [Workout];

DROP TABLE IF EXISTS [Activity];

DROP TABLE IF EXISTS [Set];

DROP TABLE IF EXISTS [Achievement];

CREATE TABLE [User] (
	[Id] [bigint] PRIMARY KEY NOT NULL,
	[Username] [nvarchar] (100) NOT NULL,
	[InsertDate] [datetime2] NOT NULL,
	UNIQUE([Username])
);

CREATE TABLE [Workout] (
	[Id] [bigint] PRIMARY KEY NOT NULL,
	[UserId] [bigint] NOT NULL,
	[Date] [datetime2] NULL,
	[Points] [int] NULL,
	[InsertDate] [datetime2] NOT NULL,
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
	[Distance] [decimal](19, 2) NULL,
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
);

CREATE INDEX [IX_Set_ActivityId] ON [Set] ([ActivityId]);

CREATE TABLE [Achievement] (
	[Id] INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
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
);

CREATE INDEX [IX_Achievement_WorkoutId] ON [Achievement] ([WorkoutId]);
