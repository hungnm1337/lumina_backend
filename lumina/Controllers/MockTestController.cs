using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.MockTest;
using DataLayer.DTOs.MockTest;
using System;
using System.Threading.Tasks;
using DataLayer.DTOs.Exam;
using System.Security.Claims;
using DataLayer.DTOs.Exam.Speaking;
using ServiceLayer.Extensions;

namespace lumina.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class MockTestController : ControllerBase
    {
        private readonly IMockTestService _mockTestService;

        public MockTestController(IMockTestService mockTestService)
        {
            _mockTestService = mockTestService;
        }

        [HttpGet("questions")]
        public async Task<ActionResult<List<ExamPartDTO>>> getMocktestInformation()
        {
            try
            {
                var result = await _mockTestService.GetMocktestAsync();
                if (result == null)
                {
                    return NotFound("No mock test information found.");
                }

                // ✅ Check user role - only Staff and Admin see correct answers
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                bool isPrivilegedUser = userRole == "Staff" || userRole == "Admin";

                // ❌ For regular users: remove correct answers and explanations
                if (!isPrivilegedUser && result != null)
                {
                    foreach (var part in result)
                    {
                        part.SanitizeForUser();
                    }
                }

                return Ok(result);
            }
            catch (Exception ex)
            { 
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("feedback/{examAttemptId}")]
        public async Task<ActionResult<MocktestFeedbackDTO>> GetMocktestFeedback(int examAttemptId)
        {
            try
            {
                if (examAttemptId <= 0)
                {
                    return BadRequest("Invalid exam attempt ID. It must be greater than 0.");
                }

                // Get userId from token
                var userId = GetUserIdFromToken();
                if (userId == null)
                {
                    return Unauthorized(new { message = "User ID not found in token." });
                }

                // Validate attempt ownership
                var validationResult = await _mockTestService.ValidateExamAttemptOwnershipAsync(examAttemptId, userId.Value);
                if (!validationResult.IsValid)
                {
                    return validationResult.ErrorType switch
                    {
                        AttemptErrorType.NotFound => NotFound(new { message = validationResult.ErrorMessage }),
                        AttemptErrorType.Forbidden => StatusCode(403, new { message = validationResult.ErrorMessage }),
                        AttemptErrorType.InvalidUser => Unauthorized(new { message = validationResult.ErrorMessage }),
                        _ => BadRequest(new { message = validationResult.ErrorMessage })
                    };
                }

                var result = await _mockTestService.GetMocktestFeedbackAsync(examAttemptId);
                if (result == null)
                {
                    return NotFound($"No feedback found for exam attempt ID: {examAttemptId}");
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
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
       
    }
}
