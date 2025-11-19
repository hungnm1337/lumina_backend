-- Migration: Add VocabularyId to UserSpacedRepetition
-- Purpose: Track spaced repetition at vocabulary word level instead of just folder level
-- Date: 2025-01-XX

-- Step 1: Add VocabularyId column (nullable)
ALTER TABLE UserSpacedRepetition
ADD VocabularyId INT NULL;

-- Step 2: Add foreign key constraint
ALTER TABLE UserSpacedRepetition
ADD CONSTRAINT FK_UserSpacedRepetition_Vocabulary
FOREIGN KEY (VocabularyId) REFERENCES Vocabulary(VocabularyId)
ON DELETE SET NULL;

-- Step 3: Add indexes for better query performance
CREATE INDEX IX_UserSpacedRepetition_VocabularyId 
ON UserSpacedRepetition(VocabularyId);

CREATE INDEX IX_UserSpacedRepetition_UserId_VocabularyId 
ON UserSpacedRepetition(UserId, VocabularyId);

CREATE INDEX IX_UserSpacedRepetition_UserId_VocabularyId_NextReviewAt 
ON UserSpacedRepetition(UserId, VocabularyId, NextReviewAt)
WHERE VocabularyId IS NOT NULL;

-- Step 4: Add unique constraint to prevent duplicate records
-- One user can only have one review record per vocabulary word
CREATE UNIQUE INDEX UQ_UserSpacedRepetition_User_Vocab
ON UserSpacedRepetition(UserId, VocabularyId)
WHERE VocabularyId IS NOT NULL;

-- Note: 
-- - VocabularyId = NULL: Track at folder level (for quiz scores)
-- - VocabularyId != NULL: Track at word level (for spaced repetition)
-- - Existing records will have VocabularyId = NULL (backward compatible)


