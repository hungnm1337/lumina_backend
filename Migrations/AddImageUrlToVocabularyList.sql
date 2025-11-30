-- =============================================
-- Script: Add ImageUrl column to VocabularyList table
-- Description: Thêm cột ImageUrl để lưu URL ảnh từ Cloudinary cho vocabulary folder
-- Date: 2025-11-28
-- =============================================

-- Kiểm tra xem cột đã tồn tại chưa (tránh lỗi nếu chạy lại)
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[VocabularyList]') 
    AND name = 'ImageUrl'
)
BEGIN
    -- Thêm cột ImageUrl (nullable, nvarchar(max))
    ALTER TABLE [dbo].[VocabularyList]
    ADD [ImageUrl] NVARCHAR(MAX) NULL;
    
    PRINT '✅ Đã thêm cột ImageUrl vào bảng VocabularyList thành công!';
END
ELSE
BEGIN
    PRINT '⚠️ Cột ImageUrl đã tồn tại trong bảng VocabularyList.';
END
GO

