using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.DTOs.Questions
{
    public class QuestionCrudDto
    {
        public int? QuestionId { get; set; } 

        public int PartId { get; set; }
        public int? PromptId { get; set; }

        public string QuestionType { get; set; }
        public string StemText { get; set; }
        public string? QuestionExplain { get; set; }
        public int ScoreWeight { get; set; } = 1;
        public int Time { get; set; } = 30;
        public List<OptionDto>? Options { get; set; }
        public string? SampleAnswer { get; set; }
    }



}
