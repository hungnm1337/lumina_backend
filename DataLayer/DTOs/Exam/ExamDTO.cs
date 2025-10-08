using DataLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.DTOs.Exam
{
    public class ExamDTO
    {
        public int ExamId { get; set; }

        public string ExamType { get; set; } = null!;

        public string Name { get; set; } = null!;

        public string Description { get; set; } = null!;

        public bool? IsActive { get; set; }

        public int CreatedBy { get; set; }

        public string? CreatedByName { get; set; }

        public int? UpdateBy { get; set; }
        public string? UpdateByName { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdateAt { get; set; }

        public List<ExamPartDTO>? ExamParts { get; set; }
    }

    public class ExamPartDTO
    {
        public int PartId { get; set; }

        public int ExamId { get; set; }

        public string PartCode { get; set; } = null!;

        public string Title { get; set; } = null!;

        public int OrderIndex { get; set; }

        public List<QuestionDTO> Questions { get; set; }

    }

    public class QuestionDTO
    {
        public int QuestionId { get; set; }

        public int PartId { get; set; }

        public string QuestionType { get; set; } = null!;

        public string StemText { get; set; } = null!;

        public int? PromptId { get; set; }

        public int ScoreWeight { get; set; }

        public string? QuestionExplain { get; set; }

        public int Time { get; set; }

        public int QuestionNumber { get; set; }

        public PromptDTO Prompt { get; set; }

        public List<OptionDTO> Options { get; set; }
    }

    public class PromptDTO
    {
        public int PromptId { get; set; }

        public int? PassageId { get; set; }

        public string Skill { get; set; } = null!;

        public string? PromptText { get; set; }

        public string? ReferenceImageUrl { get; set; }

        public string? ReferenceAudioUrl { get; set; }

        public PassageDTO  Passage { get; set; }
    }

    public class PassageDTO
    {
        public int PassageId { get; set; }
        public string Title { get; set; } = null!;
        public string ContentText { get; set; } = null!;
    }

    public class OptionDTO
    {
        public int OptionId { get; set; }

        public int QuestionId { get; set; }

        public string Content { get; set; } = null!;

        public bool? IsCorrect { get; set; }
    }
}
