using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.DTOs.Questions
{

    public class OptionDto
    {
        public int OptionId { get; set; }
        public string Content { get; set; }
        public bool IsCorrect { get; set; }
    }

    public class QuestionDto
    {
        public int QuestionId { get; set; }
        public string StemText { get; set; }
        public string QuestionExplain { get; set; }
        public int ScoreWeight { get; set; }
        public int Time { get; set; }
        public int PartId { get; set; }
        public List<OptionDto> Options { get; set; }
    }

    public class PromptDto
    {
        public int PromptId { get; set; }
        public int PartId { get; set; } 
        public string Skill { get; set; }
        public string PromptText { get; set; }
        public string ReferenceImageUrl { get; set; }
        public string ReferenceAudioUrl { get; set; }
        public List<QuestionDto> Questions { get; set; }
    }

    public class PassageDto
    {
        public int PassageId { get; set; }
        public string Title { get; set; }
        public string ContentText { get; set; }
        public PromptDto Prompt { get; set; }
    }




}
