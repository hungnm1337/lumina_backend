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
        public string Skill { get; set; } = null!; 
        public string PartLabel { get; set; } = null!; 
        public List<AIGeneratedPromptDTO> Prompts { get; set; } = new();
    }
}
