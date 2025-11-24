using DataLayer.DTOs.UserAnswer;
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

        public string? PartCode { get; set; }

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
    public class FinalizeAttemptRequestDTO
    {
        public int ExamAttemptId { get; set; }
    }

    public class SaveProgressRequestDTO
    {
        public int ExamAttemptId { get; set; }
        public int? CurrentQuestionIndex { get; set; }
    }

    public class ExamAttemptSummaryDTO
    {
        public bool Success { get; set; }
        public int ExamAttemptId { get; set; }
        public decimal TotalScore { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public int IncorrectAnswers { get; set; }
        public double PercentCorrect { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public List<UserAnswerDetailDTO> Answers { get; set; }
    }
    public class PromptDTO
    {
        public int PromptId { get; set; }

        public string Skill { get; set; } = null!;

        public string ContentText { get; set; } = null!;

        public string Title { get; set; } = null!;
        public string? ReferenceImageUrl { get; set; }
        public string? ReferenceAudioUrl { get; set; }

        
    }

    public class OptionDTO
    {
        public int? OptionId { get; set; }

        public int QuestionId { get; set; }

        public string Content { get; set; } = null!;

        public bool? IsCorrect { get; set; }
    }
}
