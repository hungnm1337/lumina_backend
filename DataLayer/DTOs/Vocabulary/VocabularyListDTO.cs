namespace DataLayer.DTOs.Vocabulary
{
    public class VocabularyListDTO
    {
        public int VocabularyListId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool? IsPublic { get; set; }
        public string MakeByName { get; set; } = string.Empty;
        public DateTime CreateAt { get; set; }
        public int VocabularyCount { get; set; } = 0; // Thêm số lượng từ vựng
        public string? Status { get; set; }
        public string? RejectionReason { get; set; }
    }
}