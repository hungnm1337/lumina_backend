using System;
using System.Collections.Generic;

namespace DataLayer.DTOs.MockTest
{
    // Request to start a mock test attempt
    public class MockTestAttemptRequestDTO
    {
        public int UserId { get; set; }
        public List<int> ExamIds { get; set; } = new List<int>();
        public string AttemptType { get; set; } = "mock_test"; // mock_test, practice, etc.
        public DateTime StartTime { get; set; }
    }

    // Response after creating mock test attempt
    public class MockTestAttemptResponseDTO
    {
        public int ExamAttemptId { get; set; }
        public int UserId { get; set; }
        public DateTime StartTime { get; set; }
        public string Status { get; set; } = "in_progress"; // in_progress, completed, scored
    }

    // Part answer submission
    public class PartAnswersSubmissionDTO
    {
        public int ExamAttemptId { get; set; }
        public int ExamId { get; set; }
        public List<PartAnswerDTO> Answers { get; set; } = new List<PartAnswerDTO>();
    }

    public class PartAnswerDTO
    {
        public int QuestionId { get; set; }
        public string UserAnswer { get; set; } = string.Empty;
        public bool? IsCorrect { get; set; }
    }

    // Complete mock test request
    public class CompleteMockTestRequestDTO
    {
        public DateTime EndTime { get; set; }
    }

    // Mock test result
    public class MockTestResultDTO
    {
        public int ExamAttemptId { get; set; }
        public int TotalScore { get; set; }
        public int ListeningScore { get; set; }
        public int ReadingScore { get; set; }
        public string SpeakingLevel { get; set; } = string.Empty;
        public string WritingLevel { get; set; } = string.Empty;
        public int CompletionTime { get; set; } // minutes
        public List<PartResultDTO> PartResults { get; set; } = new List<PartResultDTO>();
        public PerformanceAnalysisDTO? Analysis { get; set; }
    }

    public class PartResultDTO
    {
        public int ExamId { get; set; }
        public string SkillType { get; set; } = string.Empty;
        public int Score { get; set; }
        public int CorrectAnswers { get; set; }
        public int TotalQuestions { get; set; }
        public int TimeSpent { get; set; } // minutes
    }

    public class PerformanceAnalysisDTO
    {
        public List<string> Strengths { get; set; } = new List<string>();
        public List<string> Weaknesses { get; set; } = new List<string>();
        public List<string> Recommendations { get; set; } = new List<string>();
        public int? PercentileRank { get; set; }
    }
}
