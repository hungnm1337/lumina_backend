-- ============================================
-- LUMINA TOEIC - SEASON/LEADERBOARD SYSTEM
-- Migration Script
-- Version: 1.0
-- Date: 2025-10-30
-- ============================================

USE [LuminaSystem];
GO

-- ============================================
-- 1. Kiểm tra và tạo bảng Leaderboards (Seasons)
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Leaderboards')
BEGIN
    CREATE TABLE [dbo].[Leaderboards] (
        [LeaderboardId] INT PRIMARY KEY IDENTITY(1,1),
        [SeasonName] NVARCHAR(200) NULL,
        [SeasonNumber] INT NOT NULL UNIQUE,
        [StartDate] DATETIME2 NULL,
        [EndDate] DATETIME2 NULL,
        [IsActive] BIT NOT NULL DEFAULT 0,
        [CreateAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdateAt] DATETIME2 NULL
    );
    
    PRINT 'Table Leaderboards created successfully.';
END
ELSE
BEGIN
    PRINT 'Table Leaderboards already exists.';
END
GO

-- ============================================
-- 2. Kiểm tra và tạo bảng UserLeaderboards
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserLeaderboards')
BEGIN
    CREATE TABLE [dbo].[UserLeaderboards] (
        [UserLeaderboardId] INT PRIMARY KEY IDENTITY(1,1),
        [UserId] INT NOT NULL,
        [LeaderboardId] INT NOT NULL,
        [Score] INT NOT NULL DEFAULT 0,
        CONSTRAINT FK_UserLeaderboards_Users FOREIGN KEY ([UserId]) 
            REFERENCES [dbo].[Users]([UserId]) ON DELETE CASCADE,
        CONSTRAINT FK_UserLeaderboards_Leaderboards FOREIGN KEY ([LeaderboardId]) 
            REFERENCES [dbo].[Leaderboards]([LeaderboardId]) ON DELETE CASCADE,
        CONSTRAINT UQ_UserLeaderboards_UserLeaderboard UNIQUE ([UserId], [LeaderboardId])
    );
    
    PRINT 'Table UserLeaderboards created successfully.';
END
ELSE
BEGIN
    PRINT 'Table UserLeaderboards already exists.';
END
GO

-- ============================================
-- 3. Tạo Indexes cho Performance
-- ============================================

-- Index cho tìm kiếm season active
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Leaderboards_IsActive_StartDate')
BEGIN
    CREATE INDEX IX_Leaderboards_IsActive_StartDate 
    ON [dbo].[Leaderboards] ([IsActive], [StartDate]) 
    INCLUDE ([LeaderboardId], [SeasonName], [SeasonNumber], [EndDate]);
    
    PRINT 'Index IX_Leaderboards_IsActive_StartDate created.';
END
GO

-- Index cho tìm kiếm theo SeasonNumber
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Leaderboards_SeasonNumber')
BEGIN
    CREATE INDEX IX_Leaderboards_SeasonNumber 
    ON [dbo].[Leaderboards] ([SeasonNumber]);
    
    PRINT 'Index IX_Leaderboards_SeasonNumber created.';
END
GO

-- Index cho ranking (sort by score)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UserLeaderboards_LeaderboardScore')
BEGIN
    CREATE INDEX IX_UserLeaderboards_LeaderboardScore 
    ON [dbo].[UserLeaderboards] ([LeaderboardId], [Score] DESC) 
    INCLUDE ([UserId]);
    
    PRINT 'Index IX_UserLeaderboards_LeaderboardScore created.';
END
GO

-- Index cho tìm kiếm user trong season
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UserLeaderboards_UserId')
BEGIN
    CREATE INDEX IX_UserLeaderboards_UserId 
    ON [dbo].[UserLeaderboards] ([UserId]) 
    INCLUDE ([LeaderboardId], [Score]);
    
    PRINT 'Index IX_UserLeaderboards_UserId created.';
END
GO

-- ============================================
-- 4. Insert Sample Data (TEST DATA)
-- ============================================

-- Insert Season 1 (Đang diễn ra)
IF NOT EXISTS (SELECT * FROM [dbo].[Leaderboards] WHERE SeasonNumber = 1)
BEGIN
    INSERT INTO [dbo].[Leaderboards] 
        ([SeasonName], [SeasonNumber], [StartDate], [EndDate], [IsActive], [CreateAt])
    VALUES 
        (N'Spring Challenge 2025', 1, '2025-01-01', '2025-03-31', 1, GETUTCDATE());
    
    PRINT 'Season 1 (Spring Challenge 2025) created.';
END
GO

-- Insert Season 2 (Sắp diễn ra)
IF NOT EXISTS (SELECT * FROM [dbo].[Leaderboards] WHERE SeasonNumber = 2)
BEGIN
    INSERT INTO [dbo].[Leaderboards] 
        ([SeasonName], [SeasonNumber], [StartDate], [EndDate], [IsActive], [CreateAt])
    VALUES 
        (N'Summer Sprint 2025', 2, '2025-04-01', '2025-06-30', 0, GETUTCDATE());
    
    PRINT 'Season 2 (Summer Sprint 2025) created.';
END
GO

-- Insert Season 3 (Sắp diễn ra)
IF NOT EXISTS (SELECT * FROM [dbo].[Leaderboards] WHERE SeasonNumber = 3)
BEGIN
    INSERT INTO [dbo].[Leaderboards] 
        ([SeasonName], [SeasonNumber], [StartDate], [EndDate], [IsActive], [CreateAt])
    VALUES 
        (N'Autumn Masters 2025', 3, '2025-07-01', '2025-09-30', 0, GETUTCDATE());
    
    PRINT 'Season 3 (Autumn Masters 2025) created.';
END
GO

-- ============================================
-- 5. Tạo Sample UserLeaderboards (TEST DATA)
-- ============================================

DECLARE @LeaderboardId INT = (SELECT TOP 1 LeaderboardId FROM [dbo].[Leaderboards] WHERE IsActive = 1);

IF @LeaderboardId IS NOT NULL
BEGIN
    -- Lấy top 10 users để tạo sample data
    DECLARE @UserId INT;
    DECLARE @Counter INT = 1;
    
    DECLARE user_cursor CURSOR FOR 
    SELECT TOP 10 UserId FROM [dbo].[Users] ORDER BY UserId;
    
    OPEN user_cursor;
    FETCH NEXT FROM user_cursor INTO @UserId;
    
    WHILE @@FETCH_STATUS = 0 AND @Counter <= 10
    BEGIN
        IF NOT EXISTS (SELECT * FROM [dbo].[UserLeaderboards] 
                      WHERE UserId = @UserId AND LeaderboardId = @LeaderboardId)
        BEGIN
            -- Random score từ 500-15000
            DECLARE @RandomScore INT = 500 + (@Counter * 1000) + (ABS(CHECKSUM(NEWID())) % 500);
            
            INSERT INTO [dbo].[UserLeaderboards] ([UserId], [LeaderboardId], [Score])
            VALUES (@UserId, @LeaderboardId, @RandomScore);
        END
        
        SET @Counter = @Counter + 1;
        FETCH NEXT FROM user_cursor INTO @UserId;
    END
    
    CLOSE user_cursor;
    DEALLOCATE user_cursor;
    
    PRINT 'Sample UserLeaderboards data created.';
END
GO

-- ============================================
-- 6. Stored Procedures
-- ============================================

-- SP: Lấy Ranking của Season
IF OBJECT_ID('sp_GetSeasonRanking', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetSeasonRanking;
GO

CREATE PROCEDURE sp_GetSeasonRanking
    @LeaderboardId INT,
    @Top INT = 100
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT TOP (@Top)
        ROW_NUMBER() OVER (ORDER BY ul.Score DESC) AS Rank,
        ul.UserId,
        u.FullName,
        ul.Score,
        u.AvatarUrl
    FROM [dbo].[UserLeaderboards] ul
    INNER JOIN [dbo].[Users] u ON ul.UserId = u.UserId
    WHERE ul.LeaderboardId = @LeaderboardId
    ORDER BY ul.Score DESC;
END
GO

PRINT 'Stored Procedure sp_GetSeasonRanking created.';
GO

-- SP: Lấy User Rank trong Season
IF OBJECT_ID('sp_GetUserRankInSeason', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetUserRankInSeason;
GO

CREATE PROCEDURE sp_GetUserRankInSeason
    @UserId INT,
    @LeaderboardId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @UserScore INT = (
        SELECT Score 
        FROM [dbo].[UserLeaderboards]
        WHERE UserId = @UserId AND LeaderboardId = @LeaderboardId
    );
    
    IF @UserScore IS NULL
    BEGIN
        SELECT 0 AS Rank;
        RETURN;
    END
    
    SELECT COUNT(*) + 1 AS Rank
    FROM [dbo].[UserLeaderboards]
    WHERE LeaderboardId = @LeaderboardId AND Score > @UserScore;
END
GO

PRINT 'Stored Procedure sp_GetUserRankInSeason created.';
GO

-- SP: Auto Activate Seasons
IF OBJECT_ID('sp_AutoActivateSeasons', 'P') IS NOT NULL
    DROP PROCEDURE sp_AutoActivateSeasons;
GO

CREATE PROCEDURE sp_AutoActivateSeasons
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Now DATETIME2 = GETUTCDATE();
    
    -- Kích hoạt seasons đã đến ngày bắt đầu
    UPDATE [dbo].[Leaderboards]
    SET IsActive = 1, UpdateAt = @Now
    WHERE IsActive = 0 
        AND StartDate IS NOT NULL 
        AND StartDate <= @Now
        AND (EndDate IS NULL OR EndDate >= @Now);
    
    SELECT @@ROWCOUNT AS ActivatedCount;
END
GO

PRINT 'Stored Procedure sp_AutoActivateSeasons created.';
GO

-- SP: Auto End Seasons
IF OBJECT_ID('sp_AutoEndSeasons', 'P') IS NOT NULL
    DROP PROCEDURE sp_AutoEndSeasons;
GO

CREATE PROCEDURE sp_AutoEndSeasons
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Now DATETIME2 = GETUTCDATE();
    
    -- Kết thúc seasons đã hết hạn
    UPDATE [dbo].[Leaderboards]
    SET IsActive = 0, UpdateAt = @Now
    WHERE IsActive = 1 
        AND EndDate IS NOT NULL 
        AND EndDate < @Now;
    
    SELECT @@ROWCOUNT AS EndedCount;
END
GO

PRINT 'Stored Procedure sp_AutoEndSeasons created.';
GO

-- ============================================
-- 7. Views
-- ============================================

-- View: Current Season with Stats
IF OBJECT_ID('vw_CurrentSeasonStats', 'V') IS NOT NULL
    DROP VIEW vw_CurrentSeasonStats;
GO

CREATE VIEW vw_CurrentSeasonStats
AS
SELECT 
    l.LeaderboardId,
    l.SeasonName,
    l.SeasonNumber,
    l.StartDate,
    l.EndDate,
    l.IsActive,
    COUNT(DISTINCT ul.UserId) AS TotalParticipants,
    MAX(ul.Score) AS HighestScore,
    AVG(ul.Score) AS AverageScore,
    DATEDIFF(DAY, GETUTCDATE(), l.EndDate) AS DaysRemaining,
    CASE 
        WHEN GETUTCDATE() < l.StartDate THEN 'Upcoming'
        WHEN GETUTCDATE() >= l.StartDate AND GETUTCDATE() <= l.EndDate THEN 'Active'
        ELSE 'Ended'
    END AS Status
FROM [dbo].[Leaderboards] l
LEFT JOIN [dbo].[UserLeaderboards] ul ON l.LeaderboardId = ul.LeaderboardId
WHERE l.IsActive = 1
GROUP BY 
    l.LeaderboardId, l.SeasonName, l.SeasonNumber, 
    l.StartDate, l.EndDate, l.IsActive;
GO

PRINT 'View vw_CurrentSeasonStats created.';
GO

-- ============================================
-- 8. Sample Queries for Testing
-- ============================================

PRINT '';
PRINT '==============================================';
PRINT 'SAMPLE QUERIES FOR TESTING';
PRINT '==============================================';
PRINT '';
PRINT '-- 1. Lấy Season hiện tại';
PRINT 'SELECT * FROM [dbo].[Leaderboards] WHERE IsActive = 1;';
PRINT '';
PRINT '-- 2. Lấy Top 10 Ranking';
PRINT 'EXEC sp_GetSeasonRanking @LeaderboardId = 1, @Top = 10;';
PRINT '';
PRINT '-- 3. Lấy Rank của User';
PRINT 'EXEC sp_GetUserRankInSeason @UserId = 1, @LeaderboardId = 1;';
PRINT '';
PRINT '-- 4. Lấy Stats Season hiện tại';
PRINT 'SELECT * FROM vw_CurrentSeasonStats;';
PRINT '';
PRINT '-- 5. Tự động kích hoạt Seasons';
PRINT 'EXEC sp_AutoActivateSeasons;';
PRINT '';
PRINT '-- 6. Tự động kết thúc Seasons';
PRINT 'EXEC sp_AutoEndSeasons;';
PRINT '';

-- ============================================
-- 9. Verification
-- ============================================

PRINT '';
PRINT '==============================================';
PRINT 'VERIFICATION';
PRINT '==============================================';

-- Check Tables
SELECT 
    'Leaderboards' AS TableName, 
    COUNT(*) AS RecordCount 
FROM [dbo].[Leaderboards]
UNION ALL
SELECT 
    'UserLeaderboards' AS TableName, 
    COUNT(*) AS RecordCount 
FROM [dbo].[UserLeaderboards];

PRINT '';
PRINT 'Migration completed successfully!';
PRINT '==============================================';
GO
