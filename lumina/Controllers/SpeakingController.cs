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

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SpeakingController : ControllerBase
{
    private readonly ISpeakingScoringService _speakingScoringService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAzureSpeechService _azureSpeechService;

    public SpeakingController(ISpeakingScoringService speakingScoringService, IUnitOfWork unitOfWork, IAzureSpeechService azureSpeechService)
    {
        _speakingScoringService = speakingScoringService;
        _unitOfWork = unitOfWork;
        _azureSpeechService = azureSpeechService;
    }

    [HttpPost("submit-answer")]
    public async Task<IActionResult> SubmitAnswer([FromForm] SubmitSpeakingAnswerRequest request)
    {
        if (request.Audio == null || request.Audio.Length == 0)
        {
            return BadRequest("Audio file is required.");
        }

        try
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized("User ID not found in token.");
            }

            // ✅ SỬA: Sử dụng attemptId từ request thay vì tự tìm
            int attemptId;
            
            if (request.AttemptId > 0)
            {
                // Nếu frontend gửi attemptId, verify nó tồn tại và thuộc về user
                var existingAttempt = await _unitOfWork.ExamAttemptsGeneric
                    .GetAllAsync(e => e.AttemptID == request.AttemptId && e.UserID == userId)
                    .ContinueWith(t => t.Result.FirstOrDefault());
                
                if (existingAttempt == null)
                {
                    return BadRequest($"Invalid attempt ID {request.AttemptId} for current user.");
                }
                
                attemptId = request.AttemptId;
                Console.WriteLine($"[Speaking] Using provided attemptId: {attemptId}");
            }
            else
            {
                // Fallback: Tìm hoặc tạo attempt (legacy behavior)
                var question = await _unitOfWork.Questions.Get()
                                    .Include(q => q.Part)
                                    .FirstOrDefaultAsync(q => q.QuestionId == request.QuestionId);

                if (question == null)
                {
                    return NotFound("Question not found.");
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
                Console.WriteLine($"[Speaking] Created/Found attemptId: {attemptId}");
            }

            var result = await _speakingScoringService.ProcessAndScoreAnswerAsync(request.Audio, request.QuestionId, attemptId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in SubmitAnswer: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }
            Console.WriteLine(ex.StackTrace);
            return StatusCode(500, $"Internal server error: {ex.Message}");
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