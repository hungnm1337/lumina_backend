using DataLayer.Models;
using RepositoryLayer.Generic;
using System.Threading.Tasks;

namespace RepositoryLayer.Speaking
{
    /// <summary>
    /// Repository interface for Speaking module - handles all database operations for speaking exams
    /// </summary>
    public interface ISpeakingRepository : IRepository<UserAnswerSpeaking>
    {
        /// <summary>
        /// Get existing speaking answer by attemptId and questionId
        /// </summary>
        Task<UserAnswerSpeaking?> GetExistingAnswerAsync(int attemptId, int questionId);

        /// <summary>
        /// Get question with Part information
        /// </summary>
        Task<Question?> GetQuestionWithPartAsync(int questionId);

        /// <summary>
        /// Get or create exam attempt for speaking
        /// </summary>
        Task<ExamAttempt> GetOrCreateExamAttemptAsync(int userId, int examId);

        /// <summary>
        /// Get exam attempt by ID
        /// </summary>
        Task<ExamAttempt?> GetExamAttemptByIdAsync(int attemptId);

        /// <summary>
        /// Add speaking answer to database
        /// </summary>
        Task AddSpeakingAnswerAsync(UserAnswerSpeaking answer);
    }
}