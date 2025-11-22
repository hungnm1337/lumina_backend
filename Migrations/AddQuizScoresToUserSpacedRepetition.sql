-- ============================================
-- Add Quiz Scores Columns to UserSpacedRepetition
-- Migration Script
-- Date: 2025-01-19
-- ============================================

USE [LuminaSystem];
GO

-- Kiểm tra và thêm column BestQuizScore
IF NOT EXISTS (SELECT * FROM sys.columns 
               WHERE object_id = OBJECT_ID(N'[dbo].[UserSpacedRepetition]') 
               AND name = 'BestQuizScore')
BEGIN
    ALTER TABLE [dbo].[UserSpacedRepetition]
    ADD [BestQuizScore] INT NULL;
    PRINT 'Column BestQuizScore added successfully.';
END
ELSE
BEGIN
    PRINT 'Column BestQuizScore already exists.';
END
GO

-- Kiểm tra và thêm column LastQuizScore
IF NOT EXISTS (SELECT * FROM sys.columns 
               WHERE object_id = OBJECT_ID(N'[dbo].[UserSpacedRepetition]') 
               AND name = 'LastQuizScore')
BEGIN
    ALTER TABLE [dbo].[UserSpacedRepetition]
    ADD [LastQuizScore] INT NULL;
    PRINT 'Column LastQuizScore added successfully.';
END
ELSE
BEGIN
    PRINT 'Column LastQuizScore already exists.';
END
GO

-- Kiểm tra và thêm column LastQuizCompletedAt
IF NOT EXISTS (SELECT * FROM sys.columns 
               WHERE object_id = OBJECT_ID(N'[dbo].[UserSpacedRepetition]') 
               AND name = 'LastQuizCompletedAt')
BEGIN
    ALTER TABLE [dbo].[UserSpacedRepetition]
    ADD [LastQuizCompletedAt] DATETIME2 NULL;
    PRINT 'Column LastQuizCompletedAt added successfully.';
END
ELSE
BEGIN
    PRINT 'Column LastQuizCompletedAt already exists.';
END
GO

-- Kiểm tra và thêm column TotalQuizAttempts
IF NOT EXISTS (SELECT * FROM sys.columns 
               WHERE object_id = OBJECT_ID(N'[dbo].[UserSpacedRepetition]') 
               AND name = 'TotalQuizAttempts')
BEGIN
    ALTER TABLE [dbo].[UserSpacedRepetition]
    ADD [TotalQuizAttempts] INT NULL;
    PRINT 'Column TotalQuizAttempts added successfully.';
END
ELSE
BEGIN
    PRINT 'Column TotalQuizAttempts already exists.';
END
GO

PRINT 'Migration completed successfully!';
GO

