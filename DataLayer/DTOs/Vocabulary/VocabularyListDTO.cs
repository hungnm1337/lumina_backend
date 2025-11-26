namespace DataLayer.DTOs.Vocabulary
{
    public class VocabularyListDTO
    {
        public int VocabularyListId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool? IsPublic { get; set; }
        public string MakeByName { get; set; } = string.Empty;
        public int? MakeByRoleId { get; set; } // Thêm role ID của người tạo
        public DateTime CreateAt { get; set; }
        public int VocabularyCount { get; set; } = 0; // Thêm số lượng từ vựng
        public string? Status { get; set; }
        public string? RejectionReason { get; set; }
    }

    public class VocabularyListReviewRequest
    {
        public bool IsApproved { get; set; }
        public string? Comment { get; set; }
    }
}