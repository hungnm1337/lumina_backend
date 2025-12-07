using DataLayer.DTOs.Exam.Speaking;
using DataLayer.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RepositoryLayer.Speaking;
using ServiceLayer.Speech;
using System;
using System.Threading.Tasks;

namespace ServiceLayer.Exam.Speaking
{
    /// <summary>
    /// Service implementation for Speaking module - handles business logic for speaking exams
    /// </summary>
    public class SpeakingService : ISpeakingService
    {
        private readonly ISpeakingRepository _speakingRepository;
        private readonly ISpeakingScoringService _scoringService;
        private readonly IAzureSpeechService _azureSpeechService;
        private readonly ILogger<SpeakingService> _logger;

        public SpeakingService(
            ISpeakingRepository speakingRepository,
            ISpeakingScoringService scoringService,
            IAzureSpeechService azureSpeechService,
            ILogger<SpeakingService> logger)
        {
            _speakingRepository = speakingRepository;
            _scoringService = scoringService;
            _azureSpeechService = azureSpeechService;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<SpeakingSubmitResultDTO> SubmitAnswerAsync(
            IFormFile audioFile,
            int questionId,
            int attemptId,
            int userId)
        {
            try
            {
                // Check for duplicate submission
                var existing = await _speakingRepository.GetExistingAnswerAsync(attemptId, questionId);

                if (existing != null)
                {
                    _logger.LogWarning(
                        "[Speaking] Duplicate submission detected - returning existing result: QuestionId={QuestionId}, AttemptId={AttemptId}",
                        questionId,
                        attemptId
                    );

                    return new SpeakingSubmitResultDTO
                    {
                        Success = true,
                        IsDuplicate = true,
                        AttemptId = attemptId,
                        Result = MapExistingToResult(existing)
                    };
                }

                // Process and score the answer using existing scoring service
                var result = await _scoringService.ProcessAndScoreAnswerAsync(
                    audioFile,
                    questionId,
                    attemptId
                );

                return new SpeakingSubmitResultDTO
                {
                    Success = true,
                    IsDuplicate = false,
                    AttemptId = attemptId,
                    Result = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Speaking] Error submitting answer: QuestionId={QuestionId}, AttemptId={AttemptId}",
                    questionId, attemptId);

                return new SpeakingSubmitResultDTO
                {
                    Success = false,
                    AttemptId = attemptId,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <inheritdoc />
        public async Task<SpeakingSubmitResultDTO> SubmitAnswerWithAutoAttemptAsync(
            IFormFile audioFile,
            int questionId,
            int userId)
        {
            try
            {
                // Get question to find examId
                var question = await _speakingRepository.GetQuestionWithPartAsync(questionId);

                if (question == null || question.Part == null)
                {
                    return new SpeakingSubmitResultDTO
                    {
                        Success = false,
                        ErrorMessage = "Question not found."
                    };
                }

                var examId = question.Part.ExamId;

                // Get or create attempt
                var examAttempt = await _speakingRepository.GetOrCreateExamAttemptAsync(userId, examId);

                _logger.LogInformation("[Speaking] Created/Found attemptId: {AttemptId}", examAttempt.AttemptID);

                // Now submit with the attemptId
                return await SubmitAnswerAsync(audioFile, questionId, examAttempt.AttemptID, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Speaking] Error auto-creating attempt: QuestionId={QuestionId}, UserId={UserId}",
                    questionId, userId);

                return new SpeakingSubmitResultDTO
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <inheritdoc />
        public async Task<AttemptValidationResult> ValidateAttemptAsync(int attemptId, int userId)
        {
            var attempt = await _speakingRepository.GetExamAttemptByIdAsync(attemptId);

            if (attempt == null)
            {
                return new AttemptValidationResult
                {
                    IsValid = false,
                    ErrorType = AttemptErrorType.NotFound,
                    ErrorMessage = $"ExamAttempt {attemptId} not found."
                };
            }

            if (attempt.UserID != userId)
            {
                return new AttemptValidationResult
                {
                    IsValid = false,
                    ErrorType = AttemptErrorType.Forbidden,
                    ErrorMessage = "User does not own this attempt."
                };
            }

            return new AttemptValidationResult
            {
                IsValid = true,
                AttemptId = attemptId,
                ErrorType = AttemptErrorType.None
            };
        }

        /// <inheritdoc />
        public async Task<string> RecognizeSpeechFromUrlAsync(string audioUrl, string language = "en-US")
        {
            return await _azureSpeechService.RecognizeFromUrlAsync(audioUrl, language);
        }

        /// <summary>
        /// Maps existing UserAnswerSpeaking to SpeakingScoringResultDTO
        /// </summary>
        private SpeakingScoringResultDTO MapExistingToResult(UserAnswerSpeaking existing)
        {
            return new SpeakingScoringResultDTO
            {
                QuestionId = existing.QuestionId,
                Transcript = existing.Transcript ?? "",
                AudioUrl = existing.AudioUrl ?? "",
                SavedAudioUrl = existing.AudioUrl ?? "",
                PronunciationScore = (double)(existing.PronunciationScore ?? 0),
                AccuracyScore = (double)(existing.AccuracyScore ?? 0),
                FluencyScore = (double)(existing.FluencyScore ?? 0),
                CompletenessScore = (double)(existing.CompletenessScore ?? 0),
                GrammarScore = (double)(existing.GrammarScore ?? 0),
                VocabularyScore = (double)(existing.VocabularyScore ?? 0),
                ContentScore = (double)(existing.ContentScore ?? 0),
                OverallScore = (double)(existing.OverallScore ?? 0),
                SubmittedAt = DateTime.UtcNow
            };
        }
    }
}
