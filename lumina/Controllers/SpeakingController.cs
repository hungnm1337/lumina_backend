// Controllers/SpeakingController.cs

using DataLayer.DTOs.Exam.Speaking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ServiceLayer.Exam.Speaking;
using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SpeakingController : ControllerBase
{
    private readonly ISpeakingService _speakingService;
    private readonly ILogger<SpeakingController> _logger;

    public SpeakingController(
        ISpeakingService speakingService,
        ILogger<SpeakingController> logger)
    {
        _speakingService = speakingService;
        _logger = logger;
    }

    [HttpPost("submit-answer")]
    public async Task<IActionResult> SubmitAnswer([FromForm] SubmitSpeakingAnswerRequest request)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));

        // Validate audio file
        if (request.Audio == null || request.Audio.Length == 0)
        {
            return BadRequest(new { message = "Audio file is required." });
        }

        // Get userId from token
        var userId = GetUserIdFromToken();
        if (userId == null)
        {
            return Unauthorized(new { message = "User ID not found in token." });
        }

        try
        {
            SpeakingSubmitResultDTO submitResult;

            if (request.AttemptId > 0)
            {
                // Validate attempt ownership
                var validationResult = await _speakingService.ValidateAttemptAsync(request.AttemptId, userId.Value);

                if (!validationResult.IsValid)
                {
                    return HandleValidationError(validationResult);
                }

                // Submit with existing attemptId
                submitResult = await _speakingService.SubmitAnswerAsync(
                    request.Audio,
                    request.QuestionId,
                    request.AttemptId,
                    userId.Value
                );
            }
            else
            {
                // Auto-create attempt
                submitResult = await _speakingService.SubmitAnswerWithAutoAttemptAsync(
                    request.Audio,
                    request.QuestionId,
                    userId.Value
                );
            }

            // Handle result
            if (!submitResult.Success)
            {
                if (submitResult.ErrorMessage == "Question not found.")
                {
                    return NotFound(new { message = submitResult.ErrorMessage });
                }
                return StatusCode(500, new { message = submitResult.ErrorMessage });
            }

            return Ok(submitResult.Result);
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

    [HttpGet("asr-from-url")]
    [AllowAnonymous]
    public async Task<IActionResult> AsrFromUrl([FromQuery] string audioUrl, [FromQuery] string language = "en-US")
    {
        if (string.IsNullOrWhiteSpace(audioUrl))
        {
            return BadRequest(new { message = "audioUrl is required" });
        }

        var transcript = await _speakingService.RecognizeSpeechFromUrlAsync(audioUrl, language);
        return Ok(new { language, transcript });
    }

    #region Private Helper Methods

    private int? GetUserIdFromToken()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
        {
            return null;
        }
        return userId;
    }

    private IActionResult HandleValidationError(AttemptValidationResult validationResult)
    {
        return validationResult.ErrorType switch
        {
            AttemptErrorType.NotFound => NotFound(new { message = validationResult.ErrorMessage }),
            AttemptErrorType.Forbidden => Forbid(),
            AttemptErrorType.InvalidUser => Unauthorized(new { message = validationResult.ErrorMessage }),
            _ => BadRequest(new { message = validationResult.ErrorMessage })
        };
    }

    #endregion
}