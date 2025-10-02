using System.ComponentModel.DataAnnotations;

namespace DataLayer.DTOs.Vocabulary
{
    public class VocabularyListCreateDTO
    {
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        public bool IsPublic { get; set; } = false;
    }
}