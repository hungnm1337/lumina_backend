using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.DTOs.AIGeneratedExam
{
    public class AIGeneratedExamDTO
    {
        public string ExamExamTitle { get; set; } = null!;
        public string Skill { get; set; } = null!; // Listening, Reading, Speaking, Writing
        public string PartLabel { get; set; } = null!; // ví dụ: "Part 1" hoặc "Part 5"
        public List<AIGeneratedPromptDTO> Prompts { get; set; } = new();
    }
}
