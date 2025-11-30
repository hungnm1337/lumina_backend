-- =============================================
-- Script: Remove ImageUrl column from VocabularyList table
-- Description: Xóa cột ImageUrl khỏi bảng VocabularyList (đã nhầm, chỉ cần ở Vocabularies)
-- Date: 2025-11-28
-- =============================================

-- Kiểm tra xem cột có tồn tại không
IF EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[VocabularyList]') 
    AND name = 'ImageUrl'
)
BEGIN
    -- Xóa cột ImageUrl
    ALTER TABLE [dbo].[VocabularyList]
    DROP COLUMN [ImageUrl];
    
    PRINT '✅ Đã xóa cột ImageUrl khỏi bảng VocabularyList thành công!';
END
ELSE
BEGIN
    PRINT '⚠️ Cột ImageUrl không tồn tại trong bảng VocabularyList.';
END
GO

