using DataLayer.DTOs.Exam;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.DTOs.Questions
{
    public class CreatePromptWithQuestionsDTO
    {
        public PassageDTO Passage { get; set; }
        public AddPromptDTO Prompt { get; set; }
        public List<QuestionWithOptionsDTO> Questions { get; set; }
    }

    public class AddQuestionDTO
    {
        [Required]
        public int PartId { get; set; }

        [Required]
        [StringLength(50)]
        public string QuestionType { get; set; }

        [Required]
        [StringLength(4000)]
        public string StemText { get; set; }

        [Required]
        public int ScoreWeight { get; set; }

        [StringLength(2000)]
        public string QuestionExplain { get; set; }

        [Required]
        public int Time { get; set; }

        [Required]
        public int QuestionNumber { get; set; }

        [Required]
        public int PromptId { get; set; }
    }

    public class AddPromptDTO
    {

        public int? PassageId { get; set; }

        public string Skill { get; set; } = null!;

        public string? ContentText { get; set; }

        public string? ReferenceImageUrl { get; set; }

        public string? ReferenceAudioUrl { get; set; }

    }
}