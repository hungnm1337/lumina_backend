USE [LuminaSystem]
GO

PRINT '========================================';
PRINT 'Starting Notification System Migration';
PRINT '========================================';
GO

-- =============================================
-- Step 1: Create Notification table if not exists
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Notification')
BEGIN
    CREATE TABLE [dbo].[Notification] (
        [NotificationId] INT IDENTITY(1,1) NOT NULL,
        [Title] NVARCHAR(255) NOT NULL,
        [Content] NVARCHAR(MAX) NOT NULL,
        [CreatedAt] DATETIME2(7) NOT NULL DEFAULT (GETUTCDATE()),
        [CreatedBy] INT NOT NULL,
        CONSTRAINT [PK_Notification] PRIMARY KEY CLUSTERED ([NotificationId] ASC)
    );
    PRINT '✓ Created Notification table';
END
ELSE
BEGIN
    PRINT '- Notification table already exists';
    
    -- Add CreatedAt if missing
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Notification]') AND name = 'CreatedAt')
    BEGIN
        ALTER TABLE [dbo].[Notification]
        ADD [CreatedAt] DATETIME2(7) NOT NULL CONSTRAINT DF_Notification_CreatedAt DEFAULT (GETUTCDATE());
        PRINT '  ✓ Added CreatedAt column';
    END
    
    -- Add CreatedBy if missing
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Notification]') AND name = 'CreatedBy')
    BEGIN
        ALTER TABLE [dbo].[Notification]
        ADD [CreatedBy] INT NOT NULL CONSTRAINT DF_Notification_CreatedBy DEFAULT 1;
        PRINT '  ✓ Added CreatedBy column';
    END
END
GO

-- =============================================
-- Step 2: Add FK constraint for Notification.CreatedBy
-- =============================================
IF NOT EXISTS (
    SELECT * FROM sys.foreign_keys 
    WHERE parent_object_id = OBJECT_ID(N'[dbo].[Notification]')
    AND name = 'FK_Notification_User_CreatedBy'
)
BEGIN
    ALTER TABLE [dbo].[Notification]
    ADD CONSTRAINT [FK_Notification_User_CreatedBy]
    FOREIGN KEY ([CreatedBy]) REFERENCES [dbo].[Users]([UserID]);
    PRINT '✓ Added FK constraint FK_Notification_User_CreatedBy';
END
ELSE
BEGIN
    PRINT '- FK constraint FK_Notification_User_CreatedBy already exists';
END
GO

-- =============================================
-- Step 3: Create UserNotification table if not exists
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserNotification')
BEGIN
    CREATE TABLE [dbo].[UserNotification] (
        [UniqueID] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [IsRead] BIT NULL DEFAULT 0,
        [NotificationId] INT NULL,
        [CreateAt] DATETIME2(7) NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_UserNotification] PRIMARY KEY CLUSTERED ([UniqueID] ASC)
    );
    PRINT '✓ Created UserNotification table';
END
ELSE
BEGIN
    PRINT '- UserNotification table already exists';
END
GO

-- =============================================
-- Step 4: Add FK constraint for UserNotification.NotificationId
-- =============================================
IF NOT EXISTS (
    SELECT * FROM sys.foreign_keys 
    WHERE parent_object_id = OBJECT_ID(N'[dbo].[UserNotification]')
    AND name = 'FK_UserNotifications_Notifications'
)
BEGIN
    ALTER TABLE [dbo].[UserNotification]
    ADD CONSTRAINT [FK_UserNotifications_Notifications]
    FOREIGN KEY ([NotificationId]) REFERENCES [dbo].[Notification]([NotificationId]) ON DELETE CASCADE;
    PRINT '✓ Added FK constraint FK_UserNotifications_Notifications';
END
ELSE
BEGIN
    PRINT '- FK constraint FK_UserNotifications_Notifications already exists';
END
GO

-- =============================================
-- Step 5: Add FK constraint for UserNotification.UserId
-- =============================================
IF NOT EXISTS (
    SELECT * FROM sys.foreign_keys 
    WHERE parent_object_id = OBJECT_ID(N'[dbo].[UserNotification]')
    AND name = 'FK_UserNotifications_Users'
)
BEGIN
    ALTER TABLE [dbo].[UserNotification]
    ADD CONSTRAINT [FK_UserNotifications_Users]
    FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([UserID]);
    PRINT '✓ Added FK constraint FK_UserNotifications_Users';
END
ELSE
BEGIN
    PRINT '- FK constraint FK_UserNotifications_Users already exists';
END
GO

-- =============================================
-- Step 6: Create indexes for performance
-- =============================================
IF NOT EXISTS (
    SELECT * FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[Notification]')
    AND name = 'IX_Notification_CreatedAt'
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Notification_CreatedAt]
    ON [dbo].[Notification] ([CreatedAt] DESC);
    PRINT '✓ Created index IX_Notification_CreatedAt';
END
ELSE
BEGIN
    PRINT '- Index IX_Notification_CreatedAt already exists';
END
GO

IF NOT EXISTS (
    SELECT * FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[UserNotification]')
    AND name = 'IX_UserNotification_CreateAt'
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_UserNotification_CreateAt]
    ON [dbo].[UserNotification] ([CreateAt] DESC);
    PRINT '✓ Created index IX_UserNotification_CreateAt';
END
ELSE
BEGIN
    PRINT '- Index IX_UserNotification_CreateAt already exists';
END
GO

IF NOT EXISTS (
    SELECT * FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[UserNotification]')
    AND name = 'IX_UserNotification_UserId_IsRead'
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_UserNotification_UserId_IsRead]
    ON [dbo].[UserNotification] ([UserId], [IsRead])
    INCLUDE ([CreateAt], [NotificationId]);
    PRINT '✓ Created index IX_UserNotification_UserId_IsRead';
END
ELSE
BEGIN
    PRINT '- Index IX_UserNotification_UserId_IsRead already exists';
END
GO

PRINT '';
PRINT '========================================';
PRINT '✓ Migration completed successfully!';
PRINT '✓ Notification System is ready to use.';
PRINT '========================================';
GO

