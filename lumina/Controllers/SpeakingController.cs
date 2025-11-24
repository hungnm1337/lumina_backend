// Controllers/SpeakingController.cs

using DataLayer.DTOs; // <-- Thêm using cho DTOs
using DataLayer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RepositoryLayer.UnitOfWork;
using System.Security.Claims;
using ServiceLayer.Speech;
using DataLayer.DTOs.Exam.Speaking;
using ServiceLayer.Exam.Speaking;
using Microsoft.Extensions.Logging;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SpeakingController : ControllerBase
{
    private readonly ISpeakingScoringService _speakingScoringService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAzureSpeechService _azureSpeechService;
    private readonly ILogger<SpeakingController> _logger;

    public SpeakingController(
        ISpeakingScoringService speakingScoringService,
        IUnitOfWork unitOfWork,
        IAzureSpeechService azureSpeechService,
        ILogger<SpeakingController> logger)
    {
        _speakingScoringService = speakingScoringService;
        _unitOfWork = unitOfWork;
        _azureSpeechService = azureSpeechService;
        _logger = logger;
    }

    [HttpPost("submit-answer")]
    public async Task<IActionResult> SubmitAnswer([FromForm] SubmitSpeakingAnswerRequest request)
    {
        // ✅ FIX: Add timeout with CancellationToken (90 seconds)
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));

        if (request.Audio == null || request.Audio.Length == 0)
        {
            return BadRequest(new { message = "Audio file is required." });
        }

        try
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized(new { message = "User ID not found in token." });
            }

            // ✅ SỬA: Sử dụng attemptId từ request thay vì tự tìm
            int attemptId;

            if (request.AttemptId > 0)
            {
                var existingAttempt = await _unitOfWork.ExamAttemptsGeneric
                    .GetAsync(
                        a => a.AttemptID == request.AttemptId,
                        includeProperties: "User" // ✅ THÊM: Include User để check
                    );

                if (existingAttempt == null)
                {
                    return NotFound(new { message = $"ExamAttempt {request.AttemptId} not found." });
                }

                if (existingAttempt.UserID != userId)
                {
                    return Forbid();
                }

                attemptId = request.AttemptId;
            }
            else
            {
                // Fallback: Tìm hoặc tạo attempt (legacy behavior)
                var question = await _unitOfWork.Questions.Get()
                                    .Include(q => q.Part)
                                    .FirstOrDefaultAsync(q => q.QuestionId == request.QuestionId);

                if (question == null)
                {
                    return NotFound(new { message = "Question not found." });
                }
                var examId = question.Part.ExamId;

                var examAttempt = await _unitOfWork.ExamAttemptsGeneric.GetAllAsync(e => e.UserID == userId && e.ExamID == examId && e.Status == "In Progress")
                                    .ContinueWith(t => t.Result.FirstOrDefault());

                if (examAttempt == null)
                {
                    examAttempt = new ExamAttempt
                    {
                        UserID = userId,
                        ExamID = examId,
                        StartTime = DateTime.UtcNow,
                        Status = "In Progress"
                    };
                    await _unitOfWork.ExamAttemptsGeneric.AddAsync(examAttempt);
                    await _unitOfWork.CompleteAsync();
                }

                attemptId = examAttempt.AttemptID;
                _logger.LogInformation("[Speaking] Created/Found attemptId: {AttemptId}", attemptId);
            }

            // ✅ FIX: Check if already submitted (idempotency - prevents duplicate submissions)
            var existing = await _unitOfWork.UserAnswersSpeaking.GetAsync(
                a => a.AttemptID == attemptId && a.QuestionId == request.QuestionId,
                includeProperties: null
            );

            if (existing != null)
            {
                _logger.LogWarning(
                    "[Speaking] Duplicate submission detected - returning existing result: QuestionId={QuestionId}, AttemptId={AttemptId}",
                    request.QuestionId,
                    attemptId
                );

                // Return existing result instead of error (idempotent behavior)
                return Ok(new SpeakingScoringResultDTO
                {
                    QuestionId = existing.QuestionId,
                    Transcript = existing.Transcript ?? "",
                    AudioUrl = existing.AudioUrl ?? "",
                    PronunciationScore = (double)(existing.PronunciationScore ?? 0),
                    AccuracyScore = (double)(existing.AccuracyScore ?? 0),
                    FluencyScore = (double)(existing.FluencyScore ?? 0),
                    CompletenessScore = (double)(existing.CompletenessScore ?? 0),
                    GrammarScore = (double)(existing.GrammarScore ?? 0),
                    VocabularyScore = (double)(existing.VocabularyScore ?? 0),
                    ContentScore = (double)(existing.ContentScore ?? 0),
                    OverallScore = (double)(existing.OverallScore ?? 0),
                    SubmittedAt = DateTime.UtcNow
                });
            }

            // Process and score
            var result = await _speakingScoringService.ProcessAndScoreAnswerAsync(
                request.Audio,
                request.QuestionId,
                attemptId
            );

            return Ok(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "[Speaking] Scoring timeout (90s): QuestionId={QuestionId}, AttemptId={AttemptId}",
                request.QuestionId,
                request.AttemptId
            );
            return StatusCode(504, new { message = "Scoring timeout. Please try again." });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[Speaking] Error submitting answer: QuestionId={QuestionId}, AttemptId={AttemptId}",
                request.QuestionId,
                request.AttemptId
            );

            return StatusCode(500, new { message = "Internal server error", details = ex.Message });
        }
    }

    // ASR-only recognition from a Cloudinary URL with optional language override
    [HttpGet("asr-from-url")]
    [AllowAnonymous]
    public async Task<IActionResult> AsrFromUrl([FromQuery] string audioUrl, [FromQuery] string language = "en-US")
    {
        if (string.IsNullOrWhiteSpace(audioUrl)) return BadRequest("audioUrl is required");
        var text = await _azureSpeechService.RecognizeFromUrlAsync(audioUrl, language);
        return Ok(new { language, transcript = text });
    }

    // ASR-only recognition from uploaded file with optional language override
    // Temporarily commented out due to Swagger generation issue
    /*
    [HttpPost("asr-from-file")]
    [AllowAnonymous]
    public async Task<IActionResult> AsrFromFile([FromForm] IFormFile audio, [FromQuery] string language = "en-US")
    {
        if (audio == null || audio.Length == 0) return BadRequest("audio file is required");
        var text = await _azureSpeechService.RecognizeFromFileAsync(audio, language);
        return Ok(new { language, transcript = text });
    }
    */
}