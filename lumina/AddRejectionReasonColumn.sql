-- Add RejectionReason column to Articles table
-- Run this script in SQL Server Management Studio

USE LuminaSystem;
GO

-- Check if column exists
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'Articles' AND COLUMN_NAME = 'RejectionReason')
BEGIN
    -- Add the column
    ALTER TABLE Articles
    ADD RejectionReason NVARCHAR(1000) NULL;
    
    PRINT 'Column RejectionReason added successfully to Articles table';
END
ELSE
BEGIN
    PRINT 'Column RejectionReason already exists in Articles table';
END
GO

-- Verify the column was added
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Articles' AND COLUMN_NAME = 'RejectionReason';
GO
