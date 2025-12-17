using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Exam.Reading;
using DataLayer.DTOs.UserAnswer;
using Microsoft.AspNetCore.Authorization;
using DataLayer.DTOs.Exam.Speaking;
using System.Security.Claims;

namespace lumina.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ReadingController : ControllerBase
    {
        private readonly IReadingService _readingService;

        public ReadingController(IReadingService readingService)
        {
            _readingService = readingService;
        }

        [HttpPost("submit-answer")]
        public async Task<IActionResult> SubmitAnswer([FromBody] ReadingAnswerRequestDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Get userId from token
            var userId = GetUserIdFromToken();
            if (userId == null)
            {
                return Unauthorized(new { message = "User ID not found in token." });
            }

            // Validate attempt ownership
            var validationResult = await _readingService.ValidateAttemptAsync(request.ExamAttemptId, userId.Value);
            if (!validationResult.IsValid)
            {
                return HandleValidationError(validationResult);
            }

            try
            {
                var result = await _readingService.SubmitAnswerAsync(request);

                if (!result.Success)
                    return BadRequest(new { message = result.Message });

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { message = "An error occurred while submitting the answer", detail = ex.Message });
            }
        }


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

    }
}
