using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.DTOs.AIGeneratedExam
{
    public class AIGeneratedPromptDTO
    {
        public string ExamTitle { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string? ReferenceImageUrl { get; set; } // Cho Listening/Speaking
        public string? ReferenceAudioUrl { get; set; } // Cho Listening
        public List<AIGeneratedQuestionDTO> Questions { get; set; } = new();
    }
}
