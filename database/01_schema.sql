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

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026224234_InitialCreate'
)
BEGIN
    CREATE TABLE [Requests] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(200) NOT NULL,
        [OrganizationName] nvarchar(200) NOT NULL,
        [Status] tinyint NOT NULL,
        [Priority] tinyint NOT NULL,
        [AssignedTo] nvarchar(100) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Requests] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026224234_InitialCreate'
)
BEGIN
    CREATE TABLE [RequestStatusHistory] (
        [Id] int NOT NULL IDENTITY,
        [RequestId] int NOT NULL,
        [PreviousStatus] tinyint NOT NULL,
        [NewStatus] tinyint NOT NULL,
        [ChangedAt] datetime2 NOT NULL,
        [ChangedBy] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_RequestStatusHistory] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RequestStatusHistory_Requests_RequestId] FOREIGN KEY ([RequestId]) REFERENCES [Requests] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026224234_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Requests_AssignedTo] ON [Requests] ([AssignedTo]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026224234_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Requests_CreatedAt] ON [Requests] ([CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026224234_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Requests_OrganizationName] ON [Requests] ([OrganizationName]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026224234_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Requests_Priority] ON [Requests] ([Priority]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026224234_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Requests_Status_CreatedAt] ON [Requests] ([Status], [CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026224234_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RequestStatusHistory_RequestId_ChangedAt] ON [RequestStatusHistory] ([RequestId], [ChangedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026224234_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261026224234_InitialCreate', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027102921_ReplacePriorityIndexWithPriorityCreatedAt'
)
BEGIN
    DROP INDEX [IX_Requests_Priority] ON [Requests];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027102921_ReplacePriorityIndexWithPriorityCreatedAt'
)
BEGIN
    CREATE INDEX [IX_Requests_Priority_CreatedAt] ON [Requests] ([Priority], [CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027102921_ReplacePriorityIndexWithPriorityCreatedAt'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261027102921_ReplacePriorityIndexWithPriorityCreatedAt', N'8.0.11');
END;
GO

COMMIT;
GO

