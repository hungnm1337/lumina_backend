using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Analytics;
using System;
using System.Threading.Tasks;

namespace lumina.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnalyticsController : ControllerBase
    {
        private readonly IAnalyticsService _analyticsService;

        public AnalyticsController(IAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        /// <summary>
        /// Lấy key metrics tổng quan (30 ngày qua)
        /// </summary>
        [HttpGet("key-metrics")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetKeyMetrics()
        {
            try
            {
                var data = await _analyticsService.GetKeyMetricsAsync();
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy số người dùng đang online (realtime)
        /// </summary>
        [HttpGet("realtime")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetRealtimeUsers()
        {
            try
            {
                var data = await _analyticsService.GetRealtimeUsersAsync();
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy top 10 trang được xem nhiều nhất (30 ngày qua)
        /// </summary>
        [HttpGet("top-pages")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetTopPages()
        {
            try
            {
                var data = await _analyticsService.GetTopPagesAsync();
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy nguồn traffic (30 ngày qua)
        /// </summary>
        [HttpGet("traffic-sources")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetTrafficSources()
        {
            try
            {
                var data = await _analyticsService.GetTrafficSourcesAsync();
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy thống kê theo thiết bị (30 ngày qua)
        /// </summary>
        [HttpGet("devices")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetDeviceStats()
        {
            try
            {
                var data = await _analyticsService.GetDeviceStatsAsync();
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy thống kê theo quốc gia (30 ngày qua)
        /// </summary>
        [HttpGet("countries")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetCountryStats()
        {
            try
            {
                var data = await _analyticsService.GetCountryStatsAsync();
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy traffic theo ngày (30 ngày qua)
        /// </summary>
        [HttpGet("daily-traffic")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetDailyTraffic()
        {
            try
            {
                var data = await _analyticsService.GetDailyTrafficAsync();
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy thống kê theo browser (30 ngày qua)
        /// </summary>
        [HttpGet("browsers")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetBrowserStats()
        {
            try
            {
                var data = await _analyticsService.GetBrowserStatsAsync();
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}