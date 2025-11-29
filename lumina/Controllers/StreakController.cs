using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Streak;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using DataLayer.DTOs.Streak;

namespace lumina.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StreakController : ControllerBase
    {
        private readonly IStreakService _streakService;
        private readonly ILogger<StreakController> _logger;

        public StreakController(
            IStreakService streakService,
            ILogger<StreakController> logger)
        {
            _streakService = streakService;
            _logger = logger;
        }

        
        [HttpGet("summary/{userId}")]
        public async Task<IActionResult> GetStreakSummary(int userId)
        {
            try
            {
                var summary = await _streakService.GetStreakSummaryAsync(userId);
                return Ok(new
                {
                    success = true,
                    data = summary
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting streak summary for user {UserId}", userId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error"
                });
            }
        }

        
        [HttpPost("admin/trigger-daily-job")]
        // [Authorize(Roles = "Admin")] //  Bỏ comment ở production
        public IActionResult TriggerDailyJob()
        {
            try
            {
                // Enqueue job để chạy ngay lập tức
                var jobId = BackgroundJob.Enqueue<StreakBackgroundJob>(
                    job => job.ProcessDailyStreaksAsync()
                );

                _logger.LogInformation("Manual trigger daily streak job: {JobId}", jobId);

                return Ok(new
                {
                    success = true,
                    message = "Daily streak job enqueued successfully",
                    jobId = jobId,
                    dashboardUrl = "/hangfire",
                    note = "Check Hangfire Dashboard for job status"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering daily streak job");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to trigger job"
                });
            }
        }

        
        [HttpPost("admin/test-auto-process/{userId}")]
        // [Authorize(Roles = "Admin")] // ⚠️ Bỏ comment ở production
        public async Task<IActionResult> TestAutoProcess(int userId)
        {
            try
            {
                var today = _streakService.GetTodayGMT7();
                
                _logger.LogInformation("Testing auto-process for user {UserId} on date {Date}", 
                    userId, today);

                var result = await _streakService.ApplyAutoFreezeOrResetAsync(userId, today);

                return Ok(new
                {
                    success = result.Success,
                    userId = userId,
                    processDate = today,
                    eventType = result.EventType.ToString(),
                    message = result.Message,
                    summary = result.Summary
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing auto-process for user {UserId}", userId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error"
                });
            }
        }

        
        [HttpGet("admin/users-needing-process")]
        // [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUsersNeedingProcess()
        {
            try
            {
                var today = _streakService.GetTodayGMT7();
                var userIds = await _streakService.GetUsersNeedingAutoProcessAsync(today);

                return Ok(new
                {
                    success = true,
                    processDate = today,
                    count = userIds.Count,
                    userIds = userIds
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users needing process");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error"
                });
            }
        }

       
        [HttpPost("update")]
        // [Authorize] // Chỉ cho phép authenticated users
        public async Task<IActionResult> UpdateStreak([FromBody] UpdateStreakRequest request)
        {
            try
            {
                if (request.UserId <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid userId"
                    });
                }

                var today = _streakService.GetTodayGMT7();
                var result = await _streakService.UpdateOnValidPracticeAsync(
                    request.UserId, 
                    today
                );

                return Ok(new
                {
                    success = result.Success,
                    eventType = result.EventType.ToString(),
                    message = result.Message,
                    milestoneReached = result.MilestoneReached,
                    milestoneValue = result.MilestoneValue,
                    summary = result.Summary
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating streak for user {UserId}", request.UserId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error"
                });
            }
        }

        
        [HttpGet("top")]
        public async Task<IActionResult> GetTopStreakUsers()
        {
            var topUsers = await _streakService.GetTopStreakUsersAsync(10);
            return Ok(topUsers);
        }
    }

   
}
