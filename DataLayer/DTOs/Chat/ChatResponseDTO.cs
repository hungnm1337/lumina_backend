using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.DTOs.Chat
{
    public class ChatResponseDTO
    {
        public string Answer { get; set; } = null!;
        public List<string> Suggestions { get; set; } = new List<string>();
        public List<string> Examples { get; set; } = new List<string>();
        public List<string> RelatedWords { get; set; } = new List<string>();
        public string ConversationType { get; set; } = "general";
        public bool HasSaveOption { get; set; } = false;
        public string? SaveAction { get; set; }
        public List<GeneratedVocabularyDTO>? Vocabularies { get; set; }
    }

    public class GeneratedVocabularyDTO
    {
        public string Word { get; set; } = null!;
        public string Definition { get; set; } = null!;
        public string Example { get; set; } = null!;
        public string TypeOfWord { get; set; } = null!;
        public string Category { get; set; } = null!;
    }
}
