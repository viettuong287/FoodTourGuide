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
CREATE TABLE [Languages] (
    [Id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
    [Name] nvarchar(64) NOT NULL,
    [Code] nvarchar(16) NOT NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    CONSTRAINT [PK_Languages] PRIMARY KEY ([Id])
);

CREATE TABLE [Roles] (
    [Id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
    [Name] nvarchar(256) NOT NULL,
    [NormalizedName] nvarchar(256) NOT NULL,
    [ConcurrencyStamp] nvarchar(256) NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
);

CREATE TABLE [Users] (
    [Id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
    [UserName] nvarchar(256) NULL,
    [NormalizedUserName] nvarchar(256) NULL,
    [Email] nvarchar(256) NULL,
    [NormalizedEmail] nvarchar(256) NULL,
    [EmailConfirmed] bit NOT NULL DEFAULT CAST(0 AS bit),
    [PasswordHash] nvarchar(256) NULL,
    [SecurityStamp] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(256) NULL,
    [PhoneNumber] nvarchar(32) NULL,
    [PhoneNumberConfirmed] bit NOT NULL DEFAULT CAST(0 AS bit),
    [DisplayName] nvarchar(256) NULL,
    [Sex] nvarchar(16) NULL,
    [DateOfBirth] date NULL,
    [TwoFactorEnabled] bit NOT NULL DEFAULT CAST(0 AS bit),
    [LockoutEnd] datetimeoffset(3) NULL,
    [LockoutEnabled] bit NOT NULL DEFAULT CAST(1 AS bit),
    [AccessFailedCount] int NOT NULL DEFAULT 0,
    [LastLoginAt] datetime2(3) NULL,
    [CreatedAt] datetime2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt] datetime2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),
    [DeletedAt] datetime2(3) NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);

CREATE TABLE [Businesses] (
    [Id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
    [Name] nvarchar(256) NOT NULL,
    [TaxCode] nvarchar(32) NULL,
    [ContactEmail] nvarchar(256) NULL,
    [ContactPhone] nvarchar(32) NULL,
    [OwnerUserId] uniqueidentifier NULL,
    [CreatedAt] datetimeoffset(3) NOT NULL DEFAULT (SYSUTCDATETIME()),
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    CONSTRAINT [PK_Businesses] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Businesses_Users_OwnerUserId] FOREIGN KEY ([OwnerUserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
);

CREATE TABLE [BusinessOwnerProfiles] (
    [Id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
    [UserId] uniqueidentifier NOT NULL,
    [OwnerName] nvarchar(256) NULL,
    [ContactInfo] nvarchar(256) NULL,
    [CreatedAt] datetime2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_BusinessOwnerProfiles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BusinessOwnerProfiles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [EmployeeProfiles] (
    [Id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
    [UserId] uniqueidentifier NOT NULL,
    [Department] nvarchar(256) NULL,
    [Position] nvarchar(256) NULL,
    [CreatedAt] datetime2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_EmployeeProfiles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EmployeeProfiles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [UserRoles] (
    [UserId] uniqueidentifier NOT NULL,
    [RoleId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_UserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [VisitorProfiles] (
    [Id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
    [UserId] uniqueidentifier NOT NULL,
    [LanguageId] uniqueidentifier NOT NULL,
    [CreatedAt] datetime2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_VisitorProfiles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_VisitorProfiles_Languages_LanguageId] FOREIGN KEY ([LanguageId]) REFERENCES [Languages] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_VisitorProfiles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Stalls] (
    [Id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
    [BusinessId] uniqueidentifier NOT NULL,
    [Name] nvarchar(128) NOT NULL,
    [Description] nvarchar(256) NULL,
    [Slug] nvarchar(256) NOT NULL,
    [ContactEmail] nvarchar(256) NULL,
    [ContactPhone] nvarchar(16) NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [UpdatedAt] datetimeoffset NULL,
    CONSTRAINT [PK_Stalls] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Stalls_Businesses_BusinessId] FOREIGN KEY ([BusinessId]) REFERENCES [Businesses] ([Id]) ON DELETE CASCADE
);

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'IsActive', N'Name') AND [object_id] = OBJECT_ID(N'[Languages]'))
    SET IDENTITY_INSERT [Languages] ON;
INSERT INTO [Languages] ([Id], [Code], [IsActive], [Name])
VALUES ('0d2c6e75-5f4d-4c7f-97db-1c28a56d8b01', N'en', CAST(1 AS bit), N'English'),
('38f7c5e6-5d6b-4ac3-8d7d-4bfccecc2fb3', N'vi', CAST(1 AS bit), N'Vietnamese'),
('a3f2cc5c-4631-4f3c-80c2-5b4c81d7b1e1', N'ja', CAST(1 AS bit), N'Japanese');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'IsActive', N'Name') AND [object_id] = OBJECT_ID(N'[Languages]'))
    SET IDENTITY_INSERT [Languages] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ConcurrencyStamp', N'Name', N'NormalizedName') AND [object_id] = OBJECT_ID(N'[Roles]'))
    SET IDENTITY_INSERT [Roles] ON;
INSERT INTO [Roles] ([Id], [ConcurrencyStamp], [Name], [NormalizedName])
VALUES ('11111111-1111-1111-1111-111111111111', N'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', N'Admin', N'ADMIN'),
('22222222-2222-2222-2222-222222222222', N'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', N'BusinessOwner', N'BUSINESSOWNER'),
('33333333-3333-3333-3333-333333333333', N'cccccccc-cccc-cccc-cccc-cccccccccccc', N'User', N'USER');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ConcurrencyStamp', N'Name', N'NormalizedName') AND [object_id] = OBJECT_ID(N'[Roles]'))
    SET IDENTITY_INSERT [Roles] OFF;

CREATE INDEX [IX_Businesses_OwnerUserId] ON [Businesses] ([OwnerUserId]);

CREATE UNIQUE INDEX [IX_BusinessOwnerProfiles_UserId] ON [BusinessOwnerProfiles] ([UserId]);

CREATE UNIQUE INDEX [IX_EmployeeProfiles_UserId] ON [EmployeeProfiles] ([UserId]);

CREATE UNIQUE INDEX [IX_Languages_Code] ON [Languages] ([Code]);

CREATE UNIQUE INDEX [IX_Roles_NormalizedName] ON [Roles] ([NormalizedName]);

CREATE INDEX [IX_Stalls_BusinessId] ON [Stalls] ([BusinessId]);

CREATE UNIQUE INDEX [IX_Stalls_Slug] ON [Stalls] ([Slug]);

CREATE INDEX [IX_UserRoles_RoleId] ON [UserRoles] ([RoleId]);

CREATE UNIQUE INDEX [IX_Users_NormalizedUserName] ON [Users] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;

CREATE INDEX [IX_VisitorProfiles_LanguageId] ON [VisitorProfiles] ([LanguageId]);

CREATE UNIQUE INDEX [IX_VisitorProfiles_UserId] ON [VisitorProfiles] ([UserId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260314061425_InitialCreate', N'10.0.5');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [RefreshTokens] (
    [Id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
    [UserId] uniqueidentifier NOT NULL,
    [TokenHash] nvarchar(256) NOT NULL,
    [ExpiresAtUtc] datetime2(3) NOT NULL,
    [RevokedAtUtc] datetime2(3) NULL,
    [CreatedAtUtc] datetime2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),
    [DeviceId] nvarchar(128) NULL,
    [CreatedByIp] nvarchar(64) NULL,
    [ReplacedByTokenId] uniqueidentifier NULL,
    CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_RefreshTokens_TokenHash] ON [RefreshTokens] ([TokenHash]);

CREATE INDEX [IX_RefreshTokens_UserId_RevokedAtUtc] ON [RefreshTokens] ([UserId], [RevokedAtUtc]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260314172017_AddRefreshTokens', N'10.0.5');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [StallGeoFences] (
    [Id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
    [StallId] uniqueidentifier NOT NULL,
    [PolygonJson] nvarchar(max) NOT NULL,
    [MinZoom] int NULL,
    [MaxZoom] int NULL,
    CONSTRAINT [PK_StallGeoFences] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StallGeoFences_Stalls_StallId] FOREIGN KEY ([StallId]) REFERENCES [Stalls] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [StallLocations] (
    [Id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
    [StallId] uniqueidentifier NOT NULL,
    [Latitude] decimal(9,6) NOT NULL,
    [Longitude] decimal(9,6) NOT NULL,
    [RadiusMeters] decimal(10,2) NOT NULL,
    [Address] nvarchar(256) NULL,
    [MapProviderPlaceId] nvarchar(128) NULL,
    [UpdatedAt] datetimeoffset NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    CONSTRAINT [PK_StallLocations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StallLocations_Stalls_StallId] FOREIGN KEY ([StallId]) REFERENCES [Stalls] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [StallMedia] (
    [Id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
    [StallId] uniqueidentifier NOT NULL,
    [MediaUrl] nvarchar(512) NOT NULL,
    [MediaType] nvarchar(32) NOT NULL,
    [Caption] nvarchar(256) NULL,
    [SortOrder] int NOT NULL DEFAULT 0,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    CONSTRAINT [PK_StallMedia] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StallMedia_Stalls_StallId] FOREIGN KEY ([StallId]) REFERENCES [Stalls] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [StallNarrationContents] (
    [Id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
    [StallId] uniqueidentifier NOT NULL,
    [LanguageId] uniqueidentifier NOT NULL,
    [Title] nvarchar(128) NOT NULL,
    [Description] nvarchar(256) NULL,
    [ScriptText] nvarchar(max) NOT NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [UpdatedAt] datetimeoffset NULL,
    CONSTRAINT [PK_StallNarrationContents] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StallNarrationContents_Languages_LanguageId] FOREIGN KEY ([LanguageId]) REFERENCES [Languages] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StallNarrationContents_Stalls_StallId] FOREIGN KEY ([StallId]) REFERENCES [Stalls] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [VisitorLocationLogs] (
    [Id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
    [UserId] uniqueidentifier NOT NULL,
    [Latitude] decimal(9,6) NOT NULL,
    [Longitude] decimal(9,6) NOT NULL,
    [AccuracyMeters] decimal(10,2) NULL,
    [CapturedAtUtc] datetime2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_VisitorLocationLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_VisitorLocationLogs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [VisitorPreferences] (
    [Id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
    [UserId] uniqueidentifier NOT NULL,
    [LanguageId] uniqueidentifier NOT NULL,
    [Voice] nvarchar(64) NULL,
    [SpeechRate] decimal(4,2) NOT NULL,
    [AutoPlay] bit NOT NULL DEFAULT CAST(0 AS bit),
    [UpdatedAt] datetimeoffset NULL,
    CONSTRAINT [PK_VisitorPreferences] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_VisitorPreferences_Languages_LanguageId] FOREIGN KEY ([LanguageId]) REFERENCES [Languages] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_VisitorPreferences_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [NarrationAudios] (
    [Id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
    [NarrationContentId] uniqueidentifier NOT NULL,
    [AudioUrl] nvarchar(512) NULL,
    [BlobId] nvarchar(128) NULL,
    [Voice] nvarchar(64) NULL,
    [Provider] nvarchar(64) NULL,
    [DurationSeconds] int NULL,
    [IsTts] bit NOT NULL DEFAULT CAST(0 AS bit),
    [UpdatedAt] datetimeoffset NULL,
    CONSTRAINT [PK_NarrationAudios] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_NarrationAudios_StallNarrationContents_NarrationContentId] FOREIGN KEY ([NarrationContentId]) REFERENCES [StallNarrationContents] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_NarrationAudios_NarrationContentId] ON [NarrationAudios] ([NarrationContentId]);

CREATE INDEX [IX_StallGeoFences_StallId] ON [StallGeoFences] ([StallId]);

CREATE INDEX [IX_StallLocations_StallId] ON [StallLocations] ([StallId]);

CREATE INDEX [IX_StallMedia_StallId] ON [StallMedia] ([StallId]);

CREATE INDEX [IX_StallNarrationContents_LanguageId] ON [StallNarrationContents] ([LanguageId]);

CREATE INDEX [IX_StallNarrationContents_StallId] ON [StallNarrationContents] ([StallId]);

CREATE INDEX [IX_VisitorLocationLogs_UserId] ON [VisitorLocationLogs] ([UserId]);

CREATE INDEX [IX_VisitorPreferences_LanguageId] ON [VisitorPreferences] ([LanguageId]);

CREATE UNIQUE INDEX [IX_VisitorPreferences_UserId] ON [VisitorPreferences] ([UserId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260320105716_StallGeoNarrationVisitorTables', N'10.0.5');

COMMIT;
GO

BEGIN TRANSACTION;
DROP INDEX [IX_NarrationAudios_NarrationContentId] ON [NarrationAudios];

ALTER TABLE [NarrationAudios] ADD [TtsVoiceProfileId] uniqueidentifier NULL;

CREATE TABLE [TtsVoiceProfiles] (
    [Id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
    [LanguageId] uniqueidentifier NOT NULL,
    [DisplayName] nvarchar(128) NOT NULL,
    [Description] nvarchar(256) NULL,
    [VoiceName] nvarchar(128) NULL,
    [Style] nvarchar(64) NULL,
    [Role] nvarchar(64) NULL,
    [Provider] nvarchar(64) NULL,
    [IsDefault] bit NOT NULL DEFAULT CAST(0 AS bit),
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [Priority] int NOT NULL DEFAULT 0,
    CONSTRAINT [PK_TtsVoiceProfiles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TtsVoiceProfiles_Languages_LanguageId] FOREIGN KEY ([LanguageId]) REFERENCES [Languages] ([Id]) ON DELETE CASCADE
);

UPDATE [Languages] SET [Code] = N'en-US'
WHERE [Id] = '0d2c6e75-5f4d-4c7f-97db-1c28a56d8b01';
SELECT @@ROWCOUNT;


UPDATE [Languages] SET [Code] = N'vi-VN'
WHERE [Id] = '38f7c5e6-5d6b-4ac3-8d7d-4bfccecc2fb3';
SELECT @@ROWCOUNT;


UPDATE [Languages] SET [Code] = N'ja-JP'
WHERE [Id] = 'a3f2cc5c-4631-4f3c-80c2-5b4c81d7b1e1';
SELECT @@ROWCOUNT;


IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'IsActive', N'Name') AND [object_id] = OBJECT_ID(N'[Languages]'))
    SET IDENTITY_INSERT [Languages] ON;
INSERT INTO [Languages] ([Id], [Code], [IsActive], [Name])
VALUES ('06f82b17-b175-4fcf-cf56-c1de1f402c65', N'es-ES', CAST(1 AS bit), N'Spanish'),
('17a93c28-c286-40d0-d067-d2ef20513d76', N'it-IT', CAST(1 AS bit), N'Italian'),
('28ba4d39-d397-41e1-e178-e3f031624e87', N'th-TH', CAST(1 AS bit), N'Thai'),
('39cb5e4a-e4a8-42f2-f289-f40142735f98', N'id-ID', CAST(1 AS bit), N'Indonesian'),
('b1a3f6c2-6c20-4a7a-9a01-7c8f6a9b7d10', N'zh-CN', CAST(1 AS bit), N'Chinese'),
('c2b4e7d3-7d31-4b8b-8b12-8d9a7b0c8e21', N'fr-FR', CAST(1 AS bit), N'French'),
('d3c5f8e4-8e42-4c9c-9c23-9eab8c1d9f32', N'ru-RU', CAST(1 AS bit), N'Russian'),
('e4d609f5-9f53-4dad-ad34-afbc9d2e0a43', N'de-DE', CAST(1 AS bit), N'German'),
('f5e71a06-a064-4ebe-be45-b0cd0e3f1b54', N'ko-KR', CAST(1 AS bit), N'Korean');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'IsActive', N'Name') AND [object_id] = OBJECT_ID(N'[Languages]'))
    SET IDENTITY_INSERT [Languages] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayName', N'IsActive', N'IsDefault', N'LanguageId', N'Provider', N'Role', N'Style', N'VoiceName') AND [object_id] = OBJECT_ID(N'[TtsVoiceProfiles]'))
    SET IDENTITY_INSERT [TtsVoiceProfiles] ON;
INSERT INTO [TtsVoiceProfiles] ([Id], [Description], [DisplayName], [IsActive], [IsDefault], [LanguageId], [Provider], [Role], [Style], [VoiceName])
VALUES ('11111111-1111-1111-1111-111111111111', N'Giọng nam, trung tính', N'Nam Minh', CAST(1 AS bit), CAST(1 AS bit), '38f7c5e6-5d6b-4ac3-8d7d-4bfccecc2fb3', N'AzureTts', NULL, NULL, N'vi-VN-NamMinhNeural');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayName', N'IsActive', N'IsDefault', N'LanguageId', N'Provider', N'Role', N'Style', N'VoiceName') AND [object_id] = OBJECT_ID(N'[TtsVoiceProfiles]'))
    SET IDENTITY_INSERT [TtsVoiceProfiles] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayName', N'IsActive', N'LanguageId', N'Priority', N'Provider', N'Role', N'Style', N'VoiceName') AND [object_id] = OBJECT_ID(N'[TtsVoiceProfiles]'))
    SET IDENTITY_INSERT [TtsVoiceProfiles] ON;
INSERT INTO [TtsVoiceProfiles] ([Id], [Description], [DisplayName], [IsActive], [LanguageId], [Priority], [Provider], [Role], [Style], [VoiceName])
VALUES ('22222222-0001-2222-2222-222222222222', N'Giọng nữ, rõ ràng', N'Amanda', CAST(1 AS bit), '0d2c6e75-5f4d-4c7f-97db-1c28a56d8b01', 1, N'AzureTts', NULL, NULL, N'en-US-AmandaMultilingualNeural'),
('22222222-0002-2222-2222-222222222222', N'Giọng nam, ấm áp', N'Adam', CAST(1 AS bit), '0d2c6e75-5f4d-4c7f-97db-1c28a56d8b01', 1, N'AzureTts', NULL, NULL, N'en-US-AdamMultilingualNeural'),
('22222222-0003-2222-2222-222222222222', N'Giọng nữ, vui vẻ', N'Emma', CAST(1 AS bit), '0d2c6e75-5f4d-4c7f-97db-1c28a56d8b01', 1, N'AzureTts', NULL, NULL, N'en-US-EmmaMultilingualNeural'),
('22222222-0004-2222-2222-222222222222', N'Giọng nữ, trẻ trung', N'Phoebe', CAST(1 AS bit), '0d2c6e75-5f4d-4c7f-97db-1c28a56d8b01', 1, N'AzureTts', NULL, NULL, N'en-US-PhoebeMultilingualNeural');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayName', N'IsActive', N'LanguageId', N'Priority', N'Provider', N'Role', N'Style', N'VoiceName') AND [object_id] = OBJECT_ID(N'[TtsVoiceProfiles]'))
    SET IDENTITY_INSERT [TtsVoiceProfiles] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayName', N'IsActive', N'IsDefault', N'LanguageId', N'Priority', N'Provider', N'Role', N'Style', N'VoiceName') AND [object_id] = OBJECT_ID(N'[TtsVoiceProfiles]'))
    SET IDENTITY_INSERT [TtsVoiceProfiles] ON;
INSERT INTO [TtsVoiceProfiles] ([Id], [Description], [DisplayName], [IsActive], [IsDefault], [LanguageId], [Priority], [Provider], [Role], [Style], [VoiceName])
VALUES ('22222222-0005-2222-2222-222222222222', N'Giọng nữ, vui vẻ', N'Nanami', CAST(1 AS bit), CAST(1 AS bit), 'a3f2cc5c-4631-4f3c-80c2-5b4c81d7b1e1', 1, N'AzureTts', NULL, NULL, N'ja-JP-NanamiNeural');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayName', N'IsActive', N'IsDefault', N'LanguageId', N'Priority', N'Provider', N'Role', N'Style', N'VoiceName') AND [object_id] = OBJECT_ID(N'[TtsVoiceProfiles]'))
    SET IDENTITY_INSERT [TtsVoiceProfiles] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayName', N'IsActive', N'LanguageId', N'Priority', N'Provider', N'Role', N'Style', N'VoiceName') AND [object_id] = OBJECT_ID(N'[TtsVoiceProfiles]'))
    SET IDENTITY_INSERT [TtsVoiceProfiles] ON;
INSERT INTO [TtsVoiceProfiles] ([Id], [Description], [DisplayName], [IsActive], [LanguageId], [Priority], [Provider], [Role], [Style], [VoiceName])
VALUES ('22222222-0006-2222-2222-222222222222', N'Giọng nam, trung tính', N'Keita', CAST(1 AS bit), 'a3f2cc5c-4631-4f3c-80c2-5b4c81d7b1e1', 1, N'AzureTts', NULL, NULL, N'ja-JP-KeitaNeural'),
('22222222-1234-2222-2222-222222222222', N'Giọng nam, trung tính', N'Andrew', CAST(1 AS bit), '0d2c6e75-5f4d-4c7f-97db-1c28a56d8b01', 1, N'AzureTts', NULL, NULL, N'en-US-AndrewMultilingualNeural'),
('22222222-2222-2222-2222-222222222222', N'Giọng nữ, rõ ràng', N'Hoài My', CAST(1 AS bit), '38f7c5e6-5d6b-4ac3-8d7d-4bfccecc2fb3', 1, N'AzureTts', NULL, NULL, N'vi-VN-HoaiMyNeural');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayName', N'IsActive', N'LanguageId', N'Priority', N'Provider', N'Role', N'Style', N'VoiceName') AND [object_id] = OBJECT_ID(N'[TtsVoiceProfiles]'))
    SET IDENTITY_INSERT [TtsVoiceProfiles] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayName', N'IsActive', N'IsDefault', N'LanguageId', N'Priority', N'Provider', N'Role', N'Style', N'VoiceName') AND [object_id] = OBJECT_ID(N'[TtsVoiceProfiles]'))
    SET IDENTITY_INSERT [TtsVoiceProfiles] ON;
INSERT INTO [TtsVoiceProfiles] ([Id], [Description], [DisplayName], [IsActive], [IsDefault], [LanguageId], [Priority], [Provider], [Role], [Style], [VoiceName])
VALUES ('22222222-3333-2222-2222-222222222222', N'Giọng nữ, thân thiện', N'Ava', CAST(1 AS bit), CAST(1 AS bit), '0d2c6e75-5f4d-4c7f-97db-1c28a56d8b01', 1, N'AzureTts', NULL, NULL, N'en-US-AvaMultilingualNeural');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayName', N'IsActive', N'IsDefault', N'LanguageId', N'Priority', N'Provider', N'Role', N'Style', N'VoiceName') AND [object_id] = OBJECT_ID(N'[TtsVoiceProfiles]'))
    SET IDENTITY_INSERT [TtsVoiceProfiles] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayName', N'IsActive', N'LanguageId', N'Priority', N'Provider', N'Role', N'Style', N'VoiceName') AND [object_id] = OBJECT_ID(N'[TtsVoiceProfiles]'))
    SET IDENTITY_INSERT [TtsVoiceProfiles] ON;
INSERT INTO [TtsVoiceProfiles] ([Id], [Description], [DisplayName], [IsActive], [LanguageId], [Priority], [Provider], [Role], [Style], [VoiceName])
VALUES ('22222222-0007-2222-2222-222222222222', N'Giọng nữ, thân thiện', N'Xiaochen', CAST(1 AS bit), 'b1a3f6c2-6c20-4a7a-9a01-7c8f6a9b7d10', 1, N'AzureTts', NULL, NULL, N'zh-CN-XiaochenMultilingualNeural'),
('22222222-0008-2222-2222-222222222222', N'Giọng nam, vui vẻ', N'Yunxi', CAST(1 AS bit), 'b1a3f6c2-6c20-4a7a-9a01-7c8f6a9b7d10', 1, N'AzureTts', NULL, NULL, N'zh-CN-YunxiNeural');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayName', N'IsActive', N'LanguageId', N'Priority', N'Provider', N'Role', N'Style', N'VoiceName') AND [object_id] = OBJECT_ID(N'[TtsVoiceProfiles]'))
    SET IDENTITY_INSERT [TtsVoiceProfiles] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayName', N'IsActive', N'IsDefault', N'LanguageId', N'Priority', N'Provider', N'Role', N'Style', N'VoiceName') AND [object_id] = OBJECT_ID(N'[TtsVoiceProfiles]'))
    SET IDENTITY_INSERT [TtsVoiceProfiles] ON;
INSERT INTO [TtsVoiceProfiles] ([Id], [Description], [DisplayName], [IsActive], [IsDefault], [LanguageId], [Priority], [Provider], [Role], [Style], [VoiceName])
VALUES ('22222222-0009-2222-2222-222222222222', N'Giọng nữ, ám áp', N'Xiaoxiao', CAST(1 AS bit), CAST(1 AS bit), 'b1a3f6c2-6c20-4a7a-9a01-7c8f6a9b7d10', 1, N'AzureTts', NULL, NULL, N'zh-CN-XiaoxiaoNeural');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayName', N'IsActive', N'IsDefault', N'LanguageId', N'Priority', N'Provider', N'Role', N'Style', N'VoiceName') AND [object_id] = OBJECT_ID(N'[TtsVoiceProfiles]'))
    SET IDENTITY_INSERT [TtsVoiceProfiles] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayName', N'IsActive', N'LanguageId', N'Priority', N'Provider', N'Role', N'Style', N'VoiceName') AND [object_id] = OBJECT_ID(N'[TtsVoiceProfiles]'))
    SET IDENTITY_INSERT [TtsVoiceProfiles] ON;
INSERT INTO [TtsVoiceProfiles] ([Id], [Description], [DisplayName], [IsActive], [LanguageId], [Priority], [Provider], [Role], [Style], [VoiceName])
VALUES ('22222222-0010-2222-2222-222222222222', N'Giọng nam, trung tính', N'Yunjian', CAST(1 AS bit), 'b1a3f6c2-6c20-4a7a-9a01-7c8f6a9b7d10', 1, N'AzureTts', NULL, NULL, N'zh-CN-YunjianNeural');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayName', N'IsActive', N'LanguageId', N'Priority', N'Provider', N'Role', N'Style', N'VoiceName') AND [object_id] = OBJECT_ID(N'[TtsVoiceProfiles]'))
    SET IDENTITY_INSERT [TtsVoiceProfiles] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayName', N'IsActive', N'IsDefault', N'LanguageId', N'Priority', N'Provider', N'Role', N'Style', N'VoiceName') AND [object_id] = OBJECT_ID(N'[TtsVoiceProfiles]'))
    SET IDENTITY_INSERT [TtsVoiceProfiles] ON;
INSERT INTO [TtsVoiceProfiles] ([Id], [Description], [DisplayName], [IsActive], [IsDefault], [LanguageId], [Priority], [Provider], [Role], [Style], [VoiceName])
VALUES ('22222222-0011-2222-2222-222222222222', N'Giọng nữ, trung tính', N'Denise', CAST(1 AS bit), CAST(1 AS bit), 'c2b4e7d3-7d31-4b8b-8b12-8d9a7b0c8e21', 1, N'AzureTts', NULL, NULL, N'fr-FR-DeniseNeural');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayName', N'IsActive', N'IsDefault', N'LanguageId', N'Priority', N'Provider', N'Role', N'Style', N'VoiceName') AND [object_id] = OBJECT_ID(N'[TtsVoiceProfiles]'))
    SET IDENTITY_INSERT [TtsVoiceProfiles] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayName', N'IsActive', N'LanguageId', N'Priority', N'Provider', N'Role', N'Style', N'VoiceName') AND [object_id] = OBJECT_ID(N'[TtsVoiceProfiles]'))
    SET IDENTITY_INSERT [TtsVoiceProfiles] ON;
INSERT INTO [TtsVoiceProfiles] ([Id], [Description], [DisplayName], [IsActive], [LanguageId], [Priority], [Provider], [Role], [Style], [VoiceName])
VALUES ('22222222-0012-2222-2222-222222222222', N'Giọng nam, mạnh mẽ', N'Henri', CAST(1 AS bit), 'c2b4e7d3-7d31-4b8b-8b12-8d9a7b0c8e21', 1, N'AzureTts', NULL, NULL, N'fr-FR-HenriNeural');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayName', N'IsActive', N'LanguageId', N'Priority', N'Provider', N'Role', N'Style', N'VoiceName') AND [object_id] = OBJECT_ID(N'[TtsVoiceProfiles]'))
    SET IDENTITY_INSERT [TtsVoiceProfiles] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayName', N'IsActive', N'IsDefault', N'LanguageId', N'Priority', N'Provider', N'Role', N'Style', N'VoiceName') AND [object_id] = OBJECT_ID(N'[TtsVoiceProfiles]'))
    SET IDENTITY_INSERT [TtsVoiceProfiles] ON;
INSERT INTO [TtsVoiceProfiles] ([Id], [Description], [DisplayName], [IsActive], [IsDefault], [LanguageId], [Priority], [Provider], [Role], [Style], [VoiceName])
VALUES ('22222222-0013-2222-2222-222222222222', N'Giọng nữ, trung tính', N'Svetlana', CAST(1 AS bit), CAST(1 AS bit), 'd3c5f8e4-8e42-4c9c-9c23-9eab8c1d9f32', 1, N'AzureTts', NULL, NULL, N'ru-RU-SvetlanaNeural');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayName', N'IsActive', N'IsDefault', N'LanguageId', N'Priority', N'Provider', N'Role', N'Style', N'VoiceName') AND [object_id] = OBJECT_ID(N'[TtsVoiceProfiles]'))
    SET IDENTITY_INSERT [TtsVoiceProfiles] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayName', N'IsActive', N'LanguageId', N'Priority', N'Provider', N'Role', N'Style', N'VoiceName') AND [object_id] = OBJECT_ID(N'[TtsVoiceProfiles]'))
    SET IDENTITY_INSERT [TtsVoiceProfiles] ON;
INSERT INTO [TtsVoiceProfiles] ([Id], [Description], [DisplayName], [IsActive], [LanguageId], [Priority], [Provider], [Role], [Style], [VoiceName])
VALUES ('22222222-0014-2222-2222-222222222222', N'Giọng nam, trung tính', N'Dmitry', CAST(1 AS bit), 'd3c5f8e4-8e42-4c9c-9c23-9eab8c1d9f32', 1, N'AzureTts', NULL, NULL, N'ru-RU-DmitryNeural');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayName', N'IsActive', N'LanguageId', N'Priority', N'Provider', N'Role', N'Style', N'VoiceName') AND [object_id] = OBJECT_ID(N'[TtsVoiceProfiles]'))
    SET IDENTITY_INSERT [TtsVoiceProfiles] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayName', N'IsActive', N'IsDefault', N'LanguageId', N'Priority', N'Provider', N'Role', N'Style', N'VoiceName') AND [object_id] = OBJECT_ID(N'[TtsVoiceProfiles]'))
    SET IDENTITY_INSERT [TtsVoiceProfiles] ON;
INSERT INTO [TtsVoiceProfiles] ([Id], [Description], [DisplayName], [IsActive], [IsDefault], [LanguageId], [Priority], [Provider], [Role], [Style], [VoiceName])
VALUES ('22222222-0015-2222-2222-222222222222', N'Giọng nữ, trung tính', N'Katja', CAST(1 AS bit), CAST(1 AS bit), 'e4d609f5-9f53-4dad-ad34-afbc9d2e0a43', 1, N'AzureTts', NULL, NULL, N'de-DE-KatjaNeural');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayName', N'IsActive', N'IsDefault', N'LanguageId', N'Priority', N'Provider', N'Role', N'Style', N'VoiceName') AND [object_id] = OBJECT_ID(N'[TtsVoiceProfiles]'))
    SET IDENTITY_INSERT [TtsVoiceProfiles] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayName', N'IsActive', N'LanguageId', N'Priority', N'Provider', N'Role', N'Style', N'VoiceName') AND [object_id] = OBJECT_ID(N'[TtsVoiceProfiles]'))
    SET IDENTITY_INSERT [TtsVoiceProfiles] ON;
INSERT INTO [TtsVoiceProfiles] ([Id], [Description], [DisplayName], [IsActive], [LanguageId], [Priority], [Provider], [Role], [Style], [VoiceName])
VALUES ('22222222-0016-2222-2222-222222222222', N'Giọng nam, trung tính', N'Conrad', CAST(1 AS bit), 'e4d609f5-9f53-4dad-ad34-afbc9d2e0a43', 1, N'AzureTts', NULL, NULL, N'de-DE-ConradNeural');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayName', N'IsActive', N'LanguageId', N'Priority', N'Provider', N'Role', N'Style', N'VoiceName') AND [object_id] = OBJECT_ID(N'[TtsVoiceProfiles]'))
    SET IDENTITY_INSERT [TtsVoiceProfiles] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayName', N'IsActive', N'IsDefault', N'LanguageId', N'Priority', N'Provider', N'Role', N'Style', N'VoiceName') AND [object_id] = OBJECT_ID(N'[TtsVoiceProfiles]'))
    SET IDENTITY_INSERT [TtsVoiceProfiles] ON;
INSERT INTO [TtsVoiceProfiles] ([Id], [Description], [DisplayName], [IsActive], [IsDefault], [LanguageId], [Priority], [Provider], [Role], [Style], [VoiceName])
VALUES ('22222222-0017-2222-2222-222222222222', N'Giọng nữ, trung tính', N'Sun-Hi', CAST(1 AS bit), CAST(1 AS bit), 'f5e71a06-a064-4ebe-be45-b0cd0e3f1b54', 1, N'AzureTts', NULL, NULL, N'ko-KR-SunHiNeural');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayName', N'IsActive', N'IsDefault', N'LanguageId', N'Priority', N'Provider', N'Role', N'Style', N'VoiceName') AND [object_id] = OBJECT_ID(N'[TtsVoiceProfiles]'))
    SET IDENTITY_INSERT [TtsVoiceProfiles] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayName', N'IsActive', N'LanguageId', N'Priority', N'Provider', N'Role', N'Style', N'VoiceName') AND [object_id] = OBJECT_ID(N'[TtsVoiceProfiles]'))
    SET IDENTITY_INSERT [TtsVoiceProfiles] ON;
INSERT INTO [TtsVoiceProfiles] ([Id], [Description], [DisplayName], [IsActive], [LanguageId], [Priority], [Provider], [Role], [Style], [VoiceName])
VALUES ('22222222-0018-2222-2222-222222222222', N'Giọng nam, trung tính', N'InJoon', CAST(1 AS bit), 'f5e71a06-a064-4ebe-be45-b0cd0e3f1b54', 1, N'AzureTts', NULL, NULL, N'ko-KR-InJoonNeural');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayName', N'IsActive', N'LanguageId', N'Priority', N'Provider', N'Role', N'Style', N'VoiceName') AND [object_id] = OBJECT_ID(N'[TtsVoiceProfiles]'))
    SET IDENTITY_INSERT [TtsVoiceProfiles] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayName', N'IsActive', N'IsDefault', N'LanguageId', N'Priority', N'Provider', N'Role', N'Style', N'VoiceName') AND [object_id] = OBJECT_ID(N'[TtsVoiceProfiles]'))
    SET IDENTITY_INSERT [TtsVoiceProfiles] ON;
INSERT INTO [TtsVoiceProfiles] ([Id], [Description], [DisplayName], [IsActive], [IsDefault], [LanguageId], [Priority], [Provider], [Role], [Style], [VoiceName])
VALUES ('22222222-0019-2222-2222-222222222222', N'Giọng nữ, trung tính', N'Elvira', CAST(1 AS bit), CAST(1 AS bit), '06f82b17-b175-4fcf-cf56-c1de1f402c65', 1, N'AzureTts', NULL, NULL, N'es-ES-ElviraNeural');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayName', N'IsActive', N'IsDefault', N'LanguageId', N'Priority', N'Provider', N'Role', N'Style', N'VoiceName') AND [object_id] = OBJECT_ID(N'[TtsVoiceProfiles]'))
    SET IDENTITY_INSERT [TtsVoiceProfiles] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayName', N'IsActive', N'LanguageId', N'Priority', N'Provider', N'Role', N'Style', N'VoiceName') AND [object_id] = OBJECT_ID(N'[TtsVoiceProfiles]'))
    SET IDENTITY_INSERT [TtsVoiceProfiles] ON;
INSERT INTO [TtsVoiceProfiles] ([Id], [Description], [DisplayName], [IsActive], [LanguageId], [Priority], [Provider], [Role], [Style], [VoiceName])
VALUES ('22222222-0020-2222-2222-222222222222', N'Giọng nam, trung tính', N'Alvaro', CAST(1 AS bit), '06f82b17-b175-4fcf-cf56-c1de1f402c65', 1, N'AzureTts', NULL, NULL, N'es-ES-AlvaroNeural');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayName', N'IsActive', N'LanguageId', N'Priority', N'Provider', N'Role', N'Style', N'VoiceName') AND [object_id] = OBJECT_ID(N'[TtsVoiceProfiles]'))
    SET IDENTITY_INSERT [TtsVoiceProfiles] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayName', N'IsActive', N'IsDefault', N'LanguageId', N'Priority', N'Provider', N'Role', N'Style', N'VoiceName') AND [object_id] = OBJECT_ID(N'[TtsVoiceProfiles]'))
    SET IDENTITY_INSERT [TtsVoiceProfiles] ON;
INSERT INTO [TtsVoiceProfiles] ([Id], [Description], [DisplayName], [IsActive], [IsDefault], [LanguageId], [Priority], [Provider], [Role], [Style], [VoiceName])
VALUES ('22222222-0021-2222-2222-222222222222', N'Giọng nữ, trung tính', N'Elsa', CAST(1 AS bit), CAST(1 AS bit), '17a93c28-c286-40d0-d067-d2ef20513d76', 1, N'AzureTts', NULL, NULL, N'it-IT-ElsaNeural');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayName', N'IsActive', N'IsDefault', N'LanguageId', N'Priority', N'Provider', N'Role', N'Style', N'VoiceName') AND [object_id] = OBJECT_ID(N'[TtsVoiceProfiles]'))
    SET IDENTITY_INSERT [TtsVoiceProfiles] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayName', N'IsActive', N'LanguageId', N'Priority', N'Provider', N'Role', N'Style', N'VoiceName') AND [object_id] = OBJECT_ID(N'[TtsVoiceProfiles]'))
    SET IDENTITY_INSERT [TtsVoiceProfiles] ON;
INSERT INTO [TtsVoiceProfiles] ([Id], [Description], [DisplayName], [IsActive], [LanguageId], [Priority], [Provider], [Role], [Style], [VoiceName])
VALUES ('22222222-0022-2222-2222-222222222222', N'Giọng nam, trung tính', N'Diego', CAST(1 AS bit), '17a93c28-c286-40d0-d067-d2ef20513d76', 1, N'AzureTts', NULL, NULL, N'it-IT-DiegoNeural');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayName', N'IsActive', N'LanguageId', N'Priority', N'Provider', N'Role', N'Style', N'VoiceName') AND [object_id] = OBJECT_ID(N'[TtsVoiceProfiles]'))
    SET IDENTITY_INSERT [TtsVoiceProfiles] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayName', N'IsActive', N'IsDefault', N'LanguageId', N'Priority', N'Provider', N'Role', N'Style', N'VoiceName') AND [object_id] = OBJECT_ID(N'[TtsVoiceProfiles]'))
    SET IDENTITY_INSERT [TtsVoiceProfiles] ON;
INSERT INTO [TtsVoiceProfiles] ([Id], [Description], [DisplayName], [IsActive], [IsDefault], [LanguageId], [Priority], [Provider], [Role], [Style], [VoiceName])
VALUES ('22222222-0023-2222-2222-222222222222', N'Giọng nữ, trung tính', N'Premwadee', CAST(1 AS bit), CAST(1 AS bit), '28ba4d39-d397-41e1-e178-e3f031624e87', 1, N'AzureTts', NULL, NULL, N'th-TH-PremwadeeNeural');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayName', N'IsActive', N'IsDefault', N'LanguageId', N'Priority', N'Provider', N'Role', N'Style', N'VoiceName') AND [object_id] = OBJECT_ID(N'[TtsVoiceProfiles]'))
    SET IDENTITY_INSERT [TtsVoiceProfiles] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayName', N'IsActive', N'LanguageId', N'Priority', N'Provider', N'Role', N'Style', N'VoiceName') AND [object_id] = OBJECT_ID(N'[TtsVoiceProfiles]'))
    SET IDENTITY_INSERT [TtsVoiceProfiles] ON;
INSERT INTO [TtsVoiceProfiles] ([Id], [Description], [DisplayName], [IsActive], [LanguageId], [Priority], [Provider], [Role], [Style], [VoiceName])
VALUES ('22222222-0024-2222-2222-222222222222', N'Giọng nam, trung tính', N'Niwat', CAST(1 AS bit), '28ba4d39-d397-41e1-e178-e3f031624e87', 1, N'AzureTts', NULL, NULL, N'th-TH-NiwatNeural');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayName', N'IsActive', N'LanguageId', N'Priority', N'Provider', N'Role', N'Style', N'VoiceName') AND [object_id] = OBJECT_ID(N'[TtsVoiceProfiles]'))
    SET IDENTITY_INSERT [TtsVoiceProfiles] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayName', N'IsActive', N'IsDefault', N'LanguageId', N'Priority', N'Provider', N'Role', N'Style', N'VoiceName') AND [object_id] = OBJECT_ID(N'[TtsVoiceProfiles]'))
    SET IDENTITY_INSERT [TtsVoiceProfiles] ON;
INSERT INTO [TtsVoiceProfiles] ([Id], [Description], [DisplayName], [IsActive], [IsDefault], [LanguageId], [Priority], [Provider], [Role], [Style], [VoiceName])
VALUES ('22222222-0025-2222-2222-222222222222', N'Giọng nữ, trung tính', N'Gadis', CAST(1 AS bit), CAST(1 AS bit), '39cb5e4a-e4a8-42f2-f289-f40142735f98', 1, N'AzureTts', NULL, NULL, N'id-ID-GadisNeural');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayName', N'IsActive', N'IsDefault', N'LanguageId', N'Priority', N'Provider', N'Role', N'Style', N'VoiceName') AND [object_id] = OBJECT_ID(N'[TtsVoiceProfiles]'))
    SET IDENTITY_INSERT [TtsVoiceProfiles] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayName', N'IsActive', N'LanguageId', N'Priority', N'Provider', N'Role', N'Style', N'VoiceName') AND [object_id] = OBJECT_ID(N'[TtsVoiceProfiles]'))
    SET IDENTITY_INSERT [TtsVoiceProfiles] ON;
INSERT INTO [TtsVoiceProfiles] ([Id], [Description], [DisplayName], [IsActive], [LanguageId], [Priority], [Provider], [Role], [Style], [VoiceName])
VALUES ('22222222-0026-2222-2222-222222222222', N'Giọng nam, trung tính', N'Ardi', CAST(1 AS bit), '39cb5e4a-e4a8-42f2-f289-f40142735f98', 1, N'AzureTts', NULL, NULL, N'id-ID-ArdiNeural');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayName', N'IsActive', N'LanguageId', N'Priority', N'Provider', N'Role', N'Style', N'VoiceName') AND [object_id] = OBJECT_ID(N'[TtsVoiceProfiles]'))
    SET IDENTITY_INSERT [TtsVoiceProfiles] OFF;

CREATE UNIQUE INDEX [IX_NarrationAudios_NarrationContentId_TtsVoiceProfileId] ON [NarrationAudios] ([NarrationContentId], [TtsVoiceProfileId]) WHERE [TtsVoiceProfileId] IS NOT NULL;

CREATE INDEX [IX_NarrationAudios_TtsVoiceProfileId] ON [NarrationAudios] ([TtsVoiceProfileId]);

CREATE INDEX [IX_TtsVoiceProfiles_LanguageId] ON [TtsVoiceProfiles] ([LanguageId]);

ALTER TABLE [NarrationAudios] ADD CONSTRAINT [FK_NarrationAudios_TtsVoiceProfiles_TtsVoiceProfileId] FOREIGN KEY ([TtsVoiceProfileId]) REFERENCES [TtsVoiceProfiles] ([Id]) ON DELETE NO ACTION;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260322065617_TtsVoiceProfile', N'10.0.5');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Languages] ADD [DisplayName] nvarchar(64) NULL;

ALTER TABLE [Languages] ADD [FlagCode] nvarchar(8) NULL;

UPDATE [Languages] SET [DisplayName] = N'Español', [FlagCode] = N'es'
WHERE [Id] = '06f82b17-b175-4fcf-cf56-c1de1f402c65';
SELECT @@ROWCOUNT;


UPDATE [Languages] SET [DisplayName] = N'English', [FlagCode] = N'us'
WHERE [Id] = '0d2c6e75-5f4d-4c7f-97db-1c28a56d8b01';
SELECT @@ROWCOUNT;


UPDATE [Languages] SET [DisplayName] = N'Italiano', [FlagCode] = N'it'
WHERE [Id] = '17a93c28-c286-40d0-d067-d2ef20513d76';
SELECT @@ROWCOUNT;


UPDATE [Languages] SET [DisplayName] = N'ภาษาไทย', [FlagCode] = N'th'
WHERE [Id] = '28ba4d39-d397-41e1-e178-e3f031624e87';
SELECT @@ROWCOUNT;


UPDATE [Languages] SET [DisplayName] = N'Tiếng Việt', [FlagCode] = N'vn'
WHERE [Id] = '38f7c5e6-5d6b-4ac3-8d7d-4bfccecc2fb3';
SELECT @@ROWCOUNT;


UPDATE [Languages] SET [DisplayName] = N'Bahasa Indonesia', [FlagCode] = N'id'
WHERE [Id] = '39cb5e4a-e4a8-42f2-f289-f40142735f98';
SELECT @@ROWCOUNT;


UPDATE [Languages] SET [DisplayName] = N'日本語', [FlagCode] = N'jp'
WHERE [Id] = 'a3f2cc5c-4631-4f3c-80c2-5b4c81d7b1e1';
SELECT @@ROWCOUNT;


UPDATE [Languages] SET [DisplayName] = N'中文', [FlagCode] = N'cn'
WHERE [Id] = 'b1a3f6c2-6c20-4a7a-9a01-7c8f6a9b7d10';
SELECT @@ROWCOUNT;


UPDATE [Languages] SET [DisplayName] = N'Français', [FlagCode] = N'fr'
WHERE [Id] = 'c2b4e7d3-7d31-4b8b-8b12-8d9a7b0c8e21';
SELECT @@ROWCOUNT;


UPDATE [Languages] SET [DisplayName] = N'Русский', [FlagCode] = N'ru'
WHERE [Id] = 'd3c5f8e4-8e42-4c9c-9c23-9eab8c1d9f32';
SELECT @@ROWCOUNT;


UPDATE [Languages] SET [DisplayName] = N'Deutsch', [FlagCode] = N'de'
WHERE [Id] = 'e4d609f5-9f53-4dad-ad34-afbc9d2e0a43';
SELECT @@ROWCOUNT;


UPDATE [Languages] SET [DisplayName] = N'한국어', [FlagCode] = N'kr'
WHERE [Id] = 'f5e71a06-a064-4ebe-be45-b0cd0e3f1b54';
SELECT @@ROWCOUNT;


INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260325082523_AddLanguageDisplayNameAndFlagCode', N'10.0.5');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [DevicePreferences] (
    [Id] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
    [DeviceId] nvarchar(128) NOT NULL,
    [LanguageId] uniqueidentifier NOT NULL,
    [Voice] nvarchar(64) NULL,
    [SpeechRate] decimal(4,2) NOT NULL DEFAULT 1.0,
    [AutoPlay] bit NOT NULL DEFAULT CAST(1 AS bit),
    [Platform] nvarchar(32) NULL,
    [DeviceModel] nvarchar(128) NULL,
    [Manufacturer] nvarchar(128) NULL,
    [OsVersion] nvarchar(64) NULL,
    [FirstSeenAt] datetimeoffset NOT NULL,
    [LastSeenAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_DevicePreferences] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DevicePreferences_Languages_LanguageId] FOREIGN KEY ([LanguageId]) REFERENCES [Languages] ([Id]) ON DELETE NO ACTION
);

CREATE UNIQUE INDEX [IX_DevicePreferences_DeviceId] ON [DevicePreferences] ([DeviceId]);

CREATE INDEX [IX_DevicePreferences_LanguageId] ON [DevicePreferences] ([LanguageId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260326082724_AddDevicePreference', N'10.0.5');

COMMIT;
GO

