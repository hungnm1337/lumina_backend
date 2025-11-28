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

        /// <summary>
        /// Lấy số người dùng đang hoạt động và thống kê người dùng
        /// </summary>
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

        /// <summary>
        /// Lấy top N bài viết được xem nhiều nhất
        /// </summary>
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

        /// <summary>
        /// Lấy top N danh sách từ vựng được học nhiều nhất
        /// </summary>
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

        /// <summary>
        /// Lấy top N sự kiện có nhiều người tham gia nhất
        /// </summary>
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

        /// <summary>
        /// Lấy top N slide được xem nhiều nhất
        /// </summary>
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

        /// <summary>
        /// Lấy top N bài thi được làm nhiều nhất
        /// </summary>
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

        /// <summary>
        /// Lấy tỷ lệ hoàn thành bài thi
        /// </summary>
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

        /// <summary>
        /// Lấy tỷ lệ hoàn thành đọc bài viết
        /// </summary>
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

        /// <summary>
        /// Lấy tỷ lệ hoàn thành học từ vựng
        /// </summary>
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

        /// <summary>
        /// Lấy tỷ lệ tham gia sự kiện
        /// </summary>
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

        /// <summary>
        /// Lấy tổng quan tất cả analytics (overview)
        /// </summary>
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








