using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.DTOs.AIGeneratedExam
{
    public class AIGeneratedQuestionDTO
    {
        public int PartId { get; set; }
        public string QuestionType { get; set; } = null!;
        public string StemText { get; set; } = null!;
        public string? CorrectAnswer { get; set; } // dùng khi không có Option
        public string? Explanation { get; set; }
        public int ScoreWeight { get; set; }
        public int Time { get; set; }

        public List<AIGeneratedOptionDTO>? Options { get; set; } // null cho Speaking, Writing
    }
}
