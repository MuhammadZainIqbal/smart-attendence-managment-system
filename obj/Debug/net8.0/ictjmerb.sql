IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [AspNetRoles] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Institutes] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(200) NOT NULL,
    [Code] nvarchar(20) NOT NULL,
    [AdminEmail] nvarchar(256) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Institutes] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AspNetRoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Batches] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [InstituteId] int NOT NULL,
    CONSTRAINT [PK_Batches] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Batches_Institutes_InstituteId] FOREIGN KEY ([InstituteId]) REFERENCES [Institutes] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Sections] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [InstituteId] int NOT NULL,
    CONSTRAINT [PK_Sections] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Sections_Institutes_InstituteId] FOREIGN KEY ([InstituteId]) REFERENCES [Institutes] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Subjects] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(200) NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [InstituteId] int NOT NULL,
    CONSTRAINT [PK_Subjects] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Subjects_Institutes_InstituteId] FOREIGN KEY ([InstituteId]) REFERENCES [Institutes] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [AspNetUsers] (
    [Id] nvarchar(450) NOT NULL,
    [FullName] nvarchar(200) NOT NULL,
    [InstituteId] int NOT NULL,
    [IsPasswordChanged] bit NOT NULL,
    [RollNumber] nvarchar(50) NULL,
    [BatchId] int NULL,
    [SectionId] int NULL,
    [UserName] nvarchar(256) NULL,
    [NormalizedUserName] nvarchar(256) NULL,
    [Email] nvarchar(256) NULL,
    [NormalizedEmail] nvarchar(256) NULL,
    [EmailConfirmed] bit NOT NULL,
    [PasswordHash] nvarchar(max) NULL,
    [SecurityStamp] nvarchar(max) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [PhoneNumberConfirmed] bit NOT NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [LockoutEnd] datetimeoffset NULL,
    [LockoutEnabled] bit NOT NULL,
    [AccessFailedCount] int NOT NULL,
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUsers_Batches_BatchId] FOREIGN KEY ([BatchId]) REFERENCES [Batches] ([Id]),
    CONSTRAINT [FK_AspNetUsers_Institutes_InstituteId] FOREIGN KEY ([InstituteId]) REFERENCES [Institutes] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AspNetUsers_Sections_SectionId] FOREIGN KEY ([SectionId]) REFERENCES [Sections] ([Id])
);
GO

CREATE TABLE [AspNetUserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserLogins] (
    [LoginProvider] nvarchar(450) NOT NULL,
    [ProviderKey] nvarchar(450) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserRoles] (
    [UserId] nvarchar(450) NOT NULL,
    [RoleId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserTokens] (
    [UserId] nvarchar(450) NOT NULL,
    [LoginProvider] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [CourseOfferings] (
    [Id] int NOT NULL IDENTITY,
    [InstituteId] int NOT NULL,
    [TeacherId] nvarchar(450) NOT NULL,
    [SubjectId] int NOT NULL,
    [SectionId] int NOT NULL,
    [BatchId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_CourseOfferings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CourseOfferings_AspNetUsers_TeacherId] FOREIGN KEY ([TeacherId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CourseOfferings_Batches_BatchId] FOREIGN KEY ([BatchId]) REFERENCES [Batches] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CourseOfferings_Institutes_InstituteId] FOREIGN KEY ([InstituteId]) REFERENCES [Institutes] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CourseOfferings_Sections_SectionId] FOREIGN KEY ([SectionId]) REFERENCES [Sections] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CourseOfferings_Subjects_SubjectId] FOREIGN KEY ([SubjectId]) REFERENCES [Subjects] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [ClassSchedules] (
    [Id] int NOT NULL IDENTITY,
    [InstituteId] int NOT NULL,
    [CourseOfferingId] int NOT NULL,
    [DayOfWeek] int NOT NULL,
    [StartTime] time NOT NULL,
    [EndTime] time NOT NULL,
    [GracePeriodMinutes] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ClassSchedules] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ClassSchedules_CourseOfferings_CourseOfferingId] FOREIGN KEY ([CourseOfferingId]) REFERENCES [CourseOfferings] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ClassSchedules_Institutes_InstituteId] FOREIGN KEY ([InstituteId]) REFERENCES [Institutes] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [StudentEnrollments] (
    [Id] int NOT NULL IDENTITY,
    [InstituteId] int NOT NULL,
    [StudentId] nvarchar(450) NOT NULL,
    [CourseOfferingId] int NOT NULL,
    [EnrolledAt] datetime2 NOT NULL,
    CONSTRAINT [PK_StudentEnrollments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StudentEnrollments_AspNetUsers_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StudentEnrollments_CourseOfferings_CourseOfferingId] FOREIGN KEY ([CourseOfferingId]) REFERENCES [CourseOfferings] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StudentEnrollments_Institutes_InstituteId] FOREIGN KEY ([InstituteId]) REFERENCES [Institutes] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [AttendanceRecords] (
    [Id] int NOT NULL IDENTITY,
    [InstituteId] int NOT NULL,
    [StudentEnrollmentId] int NOT NULL,
    [CourseOfferingId] int NOT NULL,
    [ClassScheduleId] int NOT NULL,
    [Date] datetime2 NOT NULL,
    [Status] int NOT NULL,
    [MarkedByTeacherId] nvarchar(450) NOT NULL,
    [MarkedAt] datetime2 NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_AttendanceRecords] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AttendanceRecords_AspNetUsers_MarkedByTeacherId] FOREIGN KEY ([MarkedByTeacherId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AttendanceRecords_ClassSchedules_ClassScheduleId] FOREIGN KEY ([ClassScheduleId]) REFERENCES [ClassSchedules] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AttendanceRecords_CourseOfferings_CourseOfferingId] FOREIGN KEY ([CourseOfferingId]) REFERENCES [CourseOfferings] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AttendanceRecords_Institutes_InstituteId] FOREIGN KEY ([InstituteId]) REFERENCES [Institutes] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AttendanceRecords_StudentEnrollments_StudentEnrollmentId] FOREIGN KEY ([StudentEnrollmentId]) REFERENCES [StudentEnrollments] ([Id]) ON DELETE NO ACTION
);
GO

CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
GO

CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;
GO

CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
GO

CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
GO

CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
GO

CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
GO

CREATE INDEX [IX_AspNetUsers_BatchId] ON [AspNetUsers] ([BatchId]);
GO

CREATE INDEX [IX_AspNetUsers_InstituteId] ON [AspNetUsers] ([InstituteId]);
GO

CREATE INDEX [IX_AspNetUsers_SectionId] ON [AspNetUsers] ([SectionId]);
GO

CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;
GO

CREATE INDEX [IX_AttendanceRecords_ClassScheduleId] ON [AttendanceRecords] ([ClassScheduleId]);
GO

CREATE INDEX [IX_AttendanceRecords_CourseOfferingId] ON [AttendanceRecords] ([CourseOfferingId]);
GO

CREATE INDEX [IX_AttendanceRecords_InstituteId] ON [AttendanceRecords] ([InstituteId]);
GO

CREATE INDEX [IX_AttendanceRecords_MarkedByTeacherId] ON [AttendanceRecords] ([MarkedByTeacherId]);
GO

CREATE INDEX [IX_AttendanceRecords_StudentEnrollmentId] ON [AttendanceRecords] ([StudentEnrollmentId]);
GO

CREATE INDEX [IX_Batches_InstituteId] ON [Batches] ([InstituteId]);
GO

CREATE INDEX [IX_ClassSchedules_CourseOfferingId] ON [ClassSchedules] ([CourseOfferingId]);
GO

CREATE INDEX [IX_ClassSchedules_InstituteId] ON [ClassSchedules] ([InstituteId]);
GO

CREATE INDEX [IX_CourseOfferings_BatchId] ON [CourseOfferings] ([BatchId]);
GO

CREATE INDEX [IX_CourseOfferings_InstituteId] ON [CourseOfferings] ([InstituteId]);
GO

CREATE INDEX [IX_CourseOfferings_SectionId] ON [CourseOfferings] ([SectionId]);
GO

CREATE INDEX [IX_CourseOfferings_SubjectId] ON [CourseOfferings] ([SubjectId]);
GO

CREATE INDEX [IX_CourseOfferings_TeacherId] ON [CourseOfferings] ([TeacherId]);
GO

CREATE UNIQUE INDEX [IX_Institutes_Code] ON [Institutes] ([Code]);
GO

CREATE INDEX [IX_Sections_InstituteId] ON [Sections] ([InstituteId]);
GO

CREATE INDEX [IX_StudentEnrollments_CourseOfferingId] ON [StudentEnrollments] ([CourseOfferingId]);
GO

CREATE INDEX [IX_StudentEnrollments_InstituteId] ON [StudentEnrollments] ([InstituteId]);
GO

CREATE INDEX [IX_StudentEnrollments_StudentId] ON [StudentEnrollments] ([StudentId]);
GO

CREATE INDEX [IX_Subjects_InstituteId] ON [Subjects] ([InstituteId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251124121832_InitialCreate', N'8.0.11');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [ClassSchedules] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251124133024_AddSoftDeleteToClassSchedule', N'8.0.11');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Institutes] ADD [TimeZoneId] nvarchar(100) NOT NULL DEFAULT N'';
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251127173617_AddTimeZoneToInstitute', N'8.0.11');
GO

COMMIT;
GO

