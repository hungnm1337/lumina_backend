using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Quota;
using System.Security.Claims;

namespace lumina.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class QuotaController : ControllerBase
    {
        private readonly IQuotaService _quotaService;
        private readonly ILogger<QuotaController> _logger;

        public QuotaController(IQuotaService quotaService, ILogger<QuotaController> logger)
        {
            _quotaService = quotaService;
            _logger = logger;
        }

        /// <summary>
        /// Check if user can access a specific skill (reading, listening, speaking, writing)
        /// </summary>
        /// <param name="skill">Skill type: reading, listening, speaking, writing</param>
        [HttpGet("check/{skill}")]
        public async Task<IActionResult> CheckQuota(string skill)
        {
            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var result = await _quotaService.CheckQuotaAsync(userId, skill);

                var message = result.RequiresUpgrade
                    ? $"Nâng cấp lên Premium để truy cập {skill.ToUpper()}"
                    : result.CanAccess
                        ? "Có thể làm bài"
                        : $"Đã hết lượt. Còn {result.RemainingAttempts} lượt trong tháng này.";

                return Ok(new
                {
                    canAccess = result.CanAccess,
                    isPremium = result.IsPremium,
                    requiresUpgrade = result.RequiresUpgrade,
                    remainingAttempts = result.RemainingAttempts,
                    subscriptionType = result.SubscriptionType,
                    message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking quota for skill: {Skill}", skill);
                return StatusCode(500, new { message = "Error checking quota", error = ex.Message });
            }
        }

        /// <summary>
        /// Increment quota after user completes an exam
        /// </summary>
        /// <param name="skill">Skill type: reading, listening, speaking, writing</param>
        [HttpPost("increment/{skill}")]
        public async Task<IActionResult> IncrementQuota(string skill)
        {
            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                await _quotaService.IncrementQuotaAsync(userId, skill);

                return Ok(new { message = $"Quota incremented for {skill}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing quota for skill: {Skill}", skill);
                return StatusCode(500, new { message = "Error incrementing quota", error = ex.Message });
            }
        }

        /// <summary>
        /// Admin only: Reset all quotas (typically run monthly via scheduled job)
        /// </summary>
        [HttpPost("reset-all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ResetAllQuotas()
        {
            try
            {
                await _quotaService.ResetMonthlyQuotaAsync();
                return Ok(new { message = "All quotas reset successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting all quotas");
                return StatusCode(500, new { message = "Error resetting quotas", error = ex.Message });
            }
        }
    }
}
