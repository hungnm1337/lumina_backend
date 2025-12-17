using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ServiceLayer.Exam.Listening;
using DataLayer.DTOs.UserAnswer;
using DataLayer.DTOs.Exam.Speaking;
using System.Security.Claims;

namespace lumina.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ListeningController : ControllerBase
    {
        private readonly IListeningService _listeningService;

        public ListeningController(IListeningService listeningService)
        {
            _listeningService = listeningService;
        }

        [HttpPost("submit-answer")]
        public async Task<IActionResult> SubmitAnswer([FromBody] SubmitAnswerRequestDTO request)
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
            var validationResult = await _listeningService.ValidateAttemptAsync(request.ExamAttemptId, userId.Value);
            if (!validationResult.IsValid)
            {
                return HandleValidationError(validationResult);
            }

            try
            {
                var result = await _listeningService.SubmitAnswerAsync(request);

                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while submitting the answer.", error = ex.Message });
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
