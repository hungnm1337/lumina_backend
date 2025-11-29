-- =============================================
-- Script: Add ImageUrl column to Vocabulary table
-- Description: Thêm cột ImageUrl để lưu URL ảnh từ Cloudinary cho từng vocabulary word
-- Date: 2025-11-28
-- =============================================

-- Kiểm tra xem cột đã tồn tại chưa (tránh lỗi nếu chạy lại)
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[Vocabularies]') 
    AND name = 'ImageUrl'
)
BEGIN
    -- Thêm cột ImageUrl (nullable, nvarchar(max))
    ALTER TABLE [dbo].[Vocabularies]
    ADD [ImageUrl] NVARCHAR(MAX) NULL;
    
    PRINT '✅ Đã thêm cột ImageUrl vào bảng Vocabularies thành công!';
END
ELSE
BEGIN
    PRINT '⚠️ Cột ImageUrl đã tồn tại trong bảng Vocabularies.';
END
GO

