USE [LuminaSystem]
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[UserAnswerSpeaking]')
    AND name = 'OverallScore'
)
BEGIN
    ALTER TABLE [dbo].[UserAnswerSpeaking]
    ADD [OverallScore] DECIMAL(5,2) NULL;
END
GO

DECLARE @DuplicateCount INT
SELECT @DuplicateCount = COUNT(*)
FROM (
    SELECT AttemptID, QuestionId, COUNT(*) as DupCount
    FROM [dbo].[UserAnswerSpeaking]
    GROUP BY AttemptID, QuestionId
    HAVING COUNT(*) > 1
) AS Duplicates

IF @DuplicateCount > 0
BEGIN
    ;WITH CTE AS (
        SELECT
            UserAnswerSpeakingId,
            ROW_NUMBER() OVER (
                PARTITION BY AttemptID, QuestionId
                ORDER BY UserAnswerSpeakingId DESC
            ) AS RowNum
        FROM [dbo].[UserAnswerSpeaking]
    )
    DELETE FROM CTE WHERE RowNum > 1
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[UserAnswerSpeaking]')
    AND name = 'IX_UserAnswerSpeaking_AttemptID_QuestionId_Unique'
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [IX_UserAnswerSpeaking_AttemptID_QuestionId_Unique]
    ON [dbo].[UserAnswerSpeaking] ([AttemptID], [QuestionId]);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[UserAnswerSpeaking]')
    AND name = 'IX_UserAnswerSpeaking_OverallScore'
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_UserAnswerSpeaking_OverallScore]
    ON [dbo].[UserAnswerSpeaking] ([OverallScore])
    INCLUDE ([AttemptID], [QuestionId]);
END
GO
