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
        public string? ReferenceImageUrl { get; set; } 
        public string? ReferenceAudioUrl { get; set; } 
        public List<AIGeneratedQuestionDTO> Questions { get; set; } = new();
    }
}
