CREATE TABLE "User"
(
    "Id"         BIGINT       NOT NULL PRIMARY KEY,
    "Username"   VARCHAR(100) NOT NULL UNIQUE,
    "InsertDate" TIMESTAMP    NOT NULL,
    "UpdateDate" TIMESTAMP
);

CREATE TABLE "Workout"
(
    "Id"             BIGINT    NOT NULL PRIMARY KEY,
    "UserId"         BIGINT    NOT NULL REFERENCES "User" ON DELETE CASCADE,
    "Date"           TIMESTAMP,
    "Points"         INTEGER,
    "InsertDate"     TIMESTAMP NOT NULL,
    "UpdateDate"     TIMESTAMP,
    "ActivitiesHash" INTEGER   NOT NULL
);

CREATE INDEX "IX_Workout_UserId" ON "Workout" ("UserId");

CREATE TABLE "Activity"
(
    "Id"        BIGSERIAL    NOT NULL PRIMARY KEY,
    "WorkoutId" BIGINT       NOT NULL REFERENCES "Workout" ON DELETE CASCADE,
    "Sequence"  INTEGER      NOT NULL,
    "Name"      VARCHAR(100) NOT NULL,
    "Group"     VARCHAR(100),
    "Note"      TEXT,
    UNIQUE ("WorkoutId", "Sequence")
);

CREATE INDEX "IX_Activity_WorkoutId" ON "Activity" ("WorkoutId");

CREATE TABLE "Set"
(
    "Id"          BIGSERIAL NOT NULL PRIMARY KEY,
    "ActivityId"  BIGINT    NOT NULL REFERENCES "Activity" ON DELETE CASCADE,
    "Sequence"    INTEGER   NOT NULL,
    "Points"      INTEGER,
    "Distance"    NUMERIC(19, 2),
    "Duration"    NUMERIC(9, 1),
    "Speed"       NUMERIC(9, 2),
    "Repetitions" NUMERIC(9, 2),
    "Weight"      NUMERIC(9, 2),
    "HeartRate"   NUMERIC(9, 2),
    "Difficulty"  VARCHAR(100),
    "IsImperial"  BOOLEAN   NOT NULL,
    "IsPr"        BOOLEAN   NOT NULL,
    UNIQUE ("ActivityId", "Sequence")
);

CREATE INDEX "IX_Set_ActivityId" ON "Set" ("ActivityId");

CREATE TABLE "Achievement"
(
    "Id"          BIGSERIAL    NOT NULL PRIMARY KEY,
    "WorkoutId"   BIGINT       NOT NULL REFERENCES "Workout" ON DELETE CASCADE,
    "Type"        VARCHAR(100) NOT NULL,
    "Group"       VARCHAR(100),
    "Activity"    VARCHAR(100),
    "Distance"    NUMERIC(19, 2),
    "Duration"    NUMERIC(9, 2),
    "Speed"       NUMERIC(9, 2),
    "Repetitions" NUMERIC(9, 2),
    "Weight"      NUMERIC(9, 2),
    "CommentId"   BIGINT,
    "CommentText" TEXT,
    "IsPropped"   BOOLEAN      NOT NULL,
    "InsertDate"  TIMESTAMP    NOT NULL,
    "UpdateDate"  TIMESTAMP
);

CREATE INDEX "IX_Achievement_WorkoutId" ON "Achievement" ("WorkoutId");

CREATE VIEW "WorkoutView" AS
SELECT u."Id"         "UserId",
       u."Username",
       u."InsertDate" "UserInsertDate",
       w."Id"         "WorkoutId",
       w."Date",
       w."Points",
       w."InsertDate" "WorkoutInsertDate",
       w."ActivitiesHash"
FROM "User" u,
     "Workout" w
WHERE u."Id" = w."UserId";

CREATE VIEW "ActivityView" AS
SELECT u."Id"         "UserId",
       u."Username",
       u."InsertDate" "UserInsertDate",
       w."Id"         "WorkoutId",
       w."Date",
       w."Points",
       w."InsertDate" "WorkoutInsertDate",
       w."ActivitiesHash",
       a."Id"         "ActivityId",
       a."Sequence",
       a."Name",
       a."Group",
       a."Note"
FROM "User" u,
     "Workout" w,
     "Activity" a
WHERE u."Id" = w."UserId"
  AND w."Id" = a."WorkoutId";

CREATE VIEW "SetView" AS
SELECT u."Id"         "UserId",
       u."Username",
       u."InsertDate" "UserInsertDate",
       w."Id"         "WorkoutId",
       w."Date",
       w."Points"     "WorkoutPoints",
       w."InsertDate" "WorkoutInsertDate",
       w."ActivitiesHash",
       a."Id"         "ActivityId",
       a."Sequence"   "ActivitySequence",
       a."Name",
       a."Group",
       a."Note",
       s."Id"         "SetId",
       s."Sequence"   "SetSequence",
       s."Points"     "SetPoints",
       s."Distance",
       s."Duration",
       s."Speed",
       s."Repetitions",
       s."Weight",
       s."HeartRate",
       s."Difficulty",
       s."IsImperial",
       s."IsPr"
FROM "User" u,
     "Workout" w,
     "Activity" a,
     "Set" s
WHERE u."Id" = w."UserId"
  AND w."Id" = a."WorkoutId"
  AND a."Id" = s."ActivityId";

CREATE VIEW "AchievementView" AS
SELECT u."Id"         "UserId",
       u."Username",
       u."InsertDate" "UserInsertDate",
       w."Id"         "WorkoutId",
       w."Date",
       w."Points",
       w."InsertDate" "WorkoutInsertDate",
       w."ActivitiesHash",
       a."Id"         "AchievementId",
       a."Type",
       a."Group",
       a."Activity",
       a."Distance",
       a."Duration",
       a."Speed",
       a."Repetitions",
       a."Weight",
       a."CommentId",
       a."CommentText",
       a."IsPropped",
       a."InsertDate" "AchievementInsertDate"
FROM "User" u,
     "Workout" w,
     "Achievement" a
WHERE u."Id" = w."UserId"
  AND w."Id" = a."WorkoutId";
