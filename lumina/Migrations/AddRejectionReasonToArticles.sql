-- Migration: Add RejectionReason column to Articles table
-- Date: 2025-10-08

-- Add RejectionReason column
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'Articles' AND COLUMN_NAME = 'RejectionReason')
BEGIN
    ALTER TABLE Articles
    ADD RejectionReason NVARCHAR(1000) NULL;
    
    PRINT 'Column RejectionReason added successfully to Articles table';
END
ELSE
BEGIN
    PRINT 'Column RejectionReason already exists in Articles table';
END
GO

