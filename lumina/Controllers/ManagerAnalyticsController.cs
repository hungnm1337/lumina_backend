using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.ManagerAnalytics;
using System;
using System.Threading.Tasks;

namespace lumina.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Manager")]
    public class ManagerAnalyticsController : ControllerBase
    {
        private readonly IManagerAnalyticsService _analyticsService;

        public ManagerAnalyticsController(IManagerAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        [HttpGet("active-users")]
        public async Task<IActionResult> GetActiveUsers([FromQuery] int? days = null)
        {
            try
            {
                DateTime? fromDate = null;
                if (days.HasValue)
                {
                    fromDate = DateTime.UtcNow.AddDays(-days.Value);
                }

                var data = await _analyticsService.GetActiveUsersAsync(fromDate);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("top-articles")]
        public async Task<IActionResult> GetTopArticles([FromQuery] int topN = 10, [FromQuery] int? days = null)
        {
            try
            {
                var data = await _analyticsService.GetTopArticlesAsync(topN, days);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("top-vocabulary")]
        public async Task<IActionResult> GetTopVocabulary([FromQuery] int topN = 10, [FromQuery] int? days = null)
        {
            try
            {
                var data = await _analyticsService.GetTopVocabularyAsync(topN, days);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("top-events")]
        public async Task<IActionResult> GetTopEvents([FromQuery] int topN = 10)
        {
            try
            {
                var data = await _analyticsService.GetTopEventsAsync(topN);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("top-slides")]
        public async Task<IActionResult> GetTopSlides([FromQuery] int topN = 10, [FromQuery] int? days = null)
        {
            try
            {
                var data = await _analyticsService.GetTopSlidesAsync(topN, days);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("top-exams")]
        public async Task<IActionResult> GetTopExams([FromQuery] int topN = 10, [FromQuery] int? days = null)
        {
            try
            {
                var data = await _analyticsService.GetTopExamsAsync(topN, days);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("exam-completion-rates")]
        public async Task<IActionResult> GetExamCompletionRates(
            [FromQuery] int? examId = null,
            [FromQuery] string? examType = null,
            [FromQuery] int? days = null)
        {
            try
            {
                var data = await _analyticsService.GetExamCompletionRatesAsync(examId, examType, days);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("article-completion-rates")]
        public async Task<IActionResult> GetArticleCompletionRates(
            [FromQuery] int? articleId = null,
            [FromQuery] int? days = null)
        {
            try
            {
                var data = await _analyticsService.GetArticleCompletionRatesAsync(articleId, days);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("vocabulary-completion-rates")]
        public async Task<IActionResult> GetVocabularyCompletionRates(
            [FromQuery] int? vocabularyListId = null,
            [FromQuery] int? days = null)
        {
            try
            {
                var data = await _analyticsService.GetVocabularyCompletionRatesAsync(vocabularyListId, days);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("event-participation-rates")]
        public async Task<IActionResult> GetEventParticipationRates([FromQuery] int? eventId = null)
        {
            try
            {
                var data = await _analyticsService.GetEventParticipationRatesAsync(eventId);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview([FromQuery] int topN = 10, [FromQuery] int? days = null)
        {
            try
            {
                var data = await _analyticsService.GetOverviewAsync(topN, days);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}






























