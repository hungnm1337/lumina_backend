using DataLayer.DTOs.Exam.Speaking;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace ServiceLayer.Exam.Speaking
{
    /// <summary>
    /// Service interface for Speaking module - handles business logic for speaking exams
    /// </summary>
    public interface ISpeakingService
    {
        /// <summary>
        /// Submit and score speaking answer with existing attemptId
        /// </summary>
        /// <param name="audioFile">Audio file from user</param>
        /// <param name="questionId">Question being answered</param>
        /// <param name="attemptId">Existing attempt ID</param>
        /// <param name="userId">User submitting the answer</param>
        /// <returns>Result of the submission</returns>
        Task<SpeakingSubmitResultDTO> SubmitAnswerAsync(
            IFormFile audioFile,
            int questionId,
            int attemptId,
            int userId);

        /// <summary>
        /// Submit answer when no attemptId is provided (auto-create attempt)
        /// </summary>
        /// <param name="audioFile">Audio file from user</param>
        /// <param name="questionId">Question being answered</param>
        /// <param name="userId">User submitting the answer</param>
        /// <returns>Result of the submission</returns>
        Task<SpeakingSubmitResultDTO> SubmitAnswerWithAutoAttemptAsync(
            IFormFile audioFile,
            int questionId,
            int userId);

        /// <summary>
        /// Validate attempt ownership
        /// </summary>
        /// <param name="attemptId">Attempt to validate</param>
        /// <param name="userId">User to validate against</param>
        /// <returns>Validation result</returns>
        Task<AttemptValidationResult> ValidateAttemptAsync(int attemptId, int userId);

        /// <summary>
        /// Recognize speech from URL (ASR)
        /// </summary>
        /// <param name="audioUrl">URL of audio file</param>
        /// <param name="language">Language code (default: en-US)</param>
        /// <returns>Transcribed text</returns>
        Task<string> RecognizeSpeechFromUrlAsync(string audioUrl, string language = "en-US");
    }
}
