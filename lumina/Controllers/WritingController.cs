using DataLayer.DTOs.Exam.Speaking;
using DataLayer.DTOs.Exam.Writting;
using DataLayer.DTOs.UserAnswer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ServiceLayer.Exam.Writting;
using ServiceLayer.Quota;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace lumina.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class WritingController : ControllerBase
    {
        private readonly IWritingService _writingService;
        private readonly ILogger<WritingController> _logger;
        private readonly IQuotaService _quotaService;

        public WritingController(IWritingService writingService, ILogger<WritingController> logger, IQuotaService quotaService)
        {
            _writingService = writingService;
            _logger = logger;
            _quotaService = quotaService;
        }

        
        [HttpPost("save-answer")]
        public async Task<IActionResult> SaveWritingAnswer([FromBody] WritingAnswerRequestDTO request)
        {
            // Get userId from token
            var userId = GetUserIdFromToken();
            if (userId == null)
            {
                return Unauthorized(new { message = "User ID not found in token." });
            }

            var quotaCheck = await _quotaService.CheckQuotaAsync(userId.Value, "writing");
            
            if (!quotaCheck.CanAccess)
            {
                _logger.LogWarning(
                    "[Writing] Unauthorized access attempt - UserId: {UserId}, SubscriptionType: {SubType}, RequiresUpgrade: {RequiresUpgrade}",
                    userId.Value,
                    quotaCheck.SubscriptionType,
                    quotaCheck.RequiresUpgrade
                );
                
                return StatusCode(402, new  // 402 Payment Required
                { 
                    message = "Writing test requires Premium subscription.",
                    requiresUpgrade = quotaCheck.RequiresUpgrade,
                    subscriptionType = quotaCheck.SubscriptionType,
                    canAccess = quotaCheck.CanAccess
                });
            }

            if (!ModelState.IsValid) 
                return BadRequest(ModelState);
                
            if (request.AttemptID <= 0) 
                return BadRequest(new { Message = "Invalid AttemptID." });
                
            if (request.QuestionId <= 0) 
                return BadRequest(new { Message = "Invalid QuestionId." });
                
            if (string.IsNullOrWhiteSpace(request.UserAnswerContent)) 
                return BadRequest(new { Message = "UserAnswerContent cannot be empty." });

            // Validate attempt ownership
            var validationResult = await _writingService.ValidateAttemptAsync(request.AttemptID, userId.Value);
            if (!validationResult.IsValid)
            {
                return HandleValidationError(validationResult);
            }

            try
            {
                var result = await _writingService.SaveWritingAnswer(request);

                return result 
                    ? Ok(new { Message = "Writing answer saved successfully.", Success = true })
                    : StatusCode(StatusCodes.Status500InternalServerError, 
                        new { Message = "Failed to save writing answer.", Success = false });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while saving writing answer for AttemptID {AttemptID}, QuestionId {QuestionId}", 
                    request?.AttemptID, request?.QuestionId);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { Message = "An unexpected error occurred while saving writing answer.", Success = false });
            }
        }

        
        [HttpPost("p1-get-feedback")]
        public async Task<IActionResult> GetFeedbackP1FromAI([FromBody] WritingRequestP1DTO request)
        {
            // Get userId from token
            var userId = GetUserIdFromToken();
            if (userId == null)
            {
                return Unauthorized(new { message = "User ID not found in token." });
            }

            // Premium check for AI feedback
            var quotaCheck = await _quotaService.CheckQuotaAsync(userId.Value, "writing");
            if (!quotaCheck.CanAccess)
            {
                return StatusCode(402, new 
                { 
                    message = "Writing AI feedback requires Premium subscription.",
                    requiresUpgrade = quotaCheck.RequiresUpgrade
                });
            }

            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (string.IsNullOrWhiteSpace(request.UserAnswer))
                {
                    return BadRequest(new { Message = "UserAnswer cannot be empty." });
                }

                if (string.IsNullOrWhiteSpace(request.PictureCaption))
                {
                    return BadRequest(new { Message = "PictureCaption cannot be empty." });
                }

                var result = await _writingService.GetFeedbackP1FromAI(request);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting AI feedback for writing");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { Message = "An unexpected error occurred while getting AI feedback." });
            }
        }

        [HttpPost("p23-get-feedback")]
        public async Task<IActionResult> GetFeedbackP23FromAI([FromBody] WritingRequestP23DTO request)
        {
            // Get userId from token
            var userId = GetUserIdFromToken();
            if (userId == null)
            {
                return Unauthorized(new { message = "User ID not found in token." });
            }

            // Premium check for AI feedback
            var quotaCheck = await _quotaService.CheckQuotaAsync(userId.Value, "writing");
            if (!quotaCheck.CanAccess)
            {
                return StatusCode(402, new 
                { 
                    message = "Writing AI feedback requires Premium subscription.",
                    requiresUpgrade = quotaCheck.RequiresUpgrade
                });
            }

            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (string.IsNullOrWhiteSpace(request.UserAnswer))
                {
                    return BadRequest(new { Message = "UserAnswer cannot be empty." });
                }

                if (string.IsNullOrWhiteSpace(request.Prompt))
                {
                    return BadRequest(new { Message = "Prompt cannot be empty." });
                }

                var result = await _writingService.GetFeedbackP23FromAI(request);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting AI feedback for writing");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Message = "An unexpected error occurred while getting AI feedback." });
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
