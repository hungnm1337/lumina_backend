using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.DTOs.Chat
{
    public class SaveVocabularyRequestDTO
    {
        public int UserId { get; set; }
        public string FolderName { get; set; } = null!;
        public List<GeneratedVocabularyDTO> Vocabularies { get; set; } = new List<GeneratedVocabularyDTO>();
    }

    public class SaveVocabularyResponseDTO
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
        public int VocabularyListId { get; set; }
        public int VocabularyCount { get; set; }
    }
}
