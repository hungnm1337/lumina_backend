using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Leaderboard;
using DataLayer.DTOs;
using DataLayer.DTOs.Leaderboard;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace lumina.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LeaderboardController : ControllerBase
    {
        private readonly ILeaderboardService _service;

        public LeaderboardController(ILeaderboardService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy danh sách tất cả các mùa giải (phân trang)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PaginatedResultDTO<LeaderboardDTO>>> GetAll(
            [FromQuery] string? keyword = null, 
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetAllPaginatedAsync(keyword, page, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách mùa giải đơn giản (không phân trang)
        /// </summary>
        [HttpGet("all")]
        public async Task<ActionResult<List<LeaderboardDTO>>> GetAllSimple([FromQuery] bool? isActive = null)
        {
            var result = await _service.GetAllAsync(isActive);
            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin mùa giải hiện tại
        /// </summary>
        [HttpGet("current")]
        public async Task<ActionResult<LeaderboardDTO?>> GetCurrent()
        {
            var dto = await _service.GetCurrentAsync();
            if (dto == null) return NotFound(new { message = "Không có mùa giải nào đang diễn ra" });
            return Ok(dto);
        }

        /// <summary>
        /// Lấy thông tin mùa giải theo ID
        /// </summary>
        [HttpGet("{leaderboardId:int}")]
        public async Task<ActionResult<LeaderboardDTO>> GetById(int leaderboardId)
        {
            var dto = await _service.GetByIdAsync(leaderboardId);
            if (dto == null) return NotFound();
            return Ok(dto);
        }

        /// <summary>
        /// Tạo mùa giải mới (Chỉ Staff)
        /// </summary>
        [Authorize(Roles = "Staff")]
        [HttpPost]
        public async Task<ActionResult<int>> Create([FromBody] CreateLeaderboardDTO dto)
        {
            try
            {
                var id = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { leaderboardId = id }, id);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật thông tin mùa giải (Chỉ Staff)
        /// </summary>
        [Authorize(Roles = "Staff")]
        [HttpPut("{leaderboardId:int}")]
        public async Task<ActionResult> Update(int leaderboardId, [FromBody] UpdateLeaderboardDTO dto)
        {
            try
            {
                var ok = await _service.UpdateAsync(leaderboardId, dto);
                if (!ok) return NotFound();
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Xóa mùa giải (Chỉ Staff)
        /// </summary>
        [Authorize(Roles = "Staff")]
        [HttpDelete("{leaderboardId:int}")]
        public async Task<ActionResult> Delete(int leaderboardId)
        {
            var ok = await _service.DeleteAsync(leaderboardId);
            if (!ok) return NotFound();
            return NoContent();
        }

        /// <summary>
        /// Đặt mùa giải thành mùa hiện tại (Chỉ Staff)
        /// </summary>
        [Authorize(Roles = "Staff")]
        [HttpPost("{leaderboardId:int}/set-current")]
        public async Task<ActionResult> SetCurrent(int leaderboardId)
        {
            var ok = await _service.SetCurrentAsync(leaderboardId);
            if (!ok) return NotFound();
            return NoContent();
        }

        /// <summary>
        /// Lấy bảng xếp hạng của mùa giải
        /// </summary>
        [HttpGet("{leaderboardId:int}/ranking")]
        public async Task<ActionResult<List<LeaderboardRankDTO>>> GetRanking(
            int leaderboardId, 
            [FromQuery] int top = 100)
        {
            var result = await _service.GetSeasonRankingAsync(leaderboardId, top);
            return Ok(result);
        }

        /// <summary>
        /// Tính lại điểm của mùa giải (Chỉ Staff)
        /// </summary>
        [Authorize(Roles = "Staff")]
        [HttpPost("{leaderboardId:int}/recalculate")]
        public async Task<ActionResult<int>> Recalculate(int leaderboardId)
        {
            var affected = await _service.RecalculateSeasonScoresAsync(leaderboardId);
            return Ok(new { affected, message = $"Đã tính lại điểm cho {affected} người dùng" });
        }

        /// <summary>
        /// Reset điểm của mùa giải (kết thúc mùa) (Chỉ Staff)
        /// </summary>
        [Authorize(Roles = "Staff")]
        [HttpPost("{leaderboardId:int}/reset")]
        public async Task<ActionResult<int>> ResetSeason(
            int leaderboardId, 
            [FromQuery] bool archiveScores = true)
        {
            var affected = await _service.ResetSeasonAsync(leaderboardId, archiveScores);
            return Ok(new { affected, message = $"Đã reset điểm cho mùa giải. {affected} bản ghi bị xóa" });
        }

        /// <summary>
        /// Lấy thống kê của user trong mùa giải hiện tại
        /// </summary>
        [Authorize]
        [HttpGet("user/stats")]
        public async Task<ActionResult<UserSeasonStatsDTO>> GetMyStats([FromQuery] int? leaderboardId = null)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Không thể xác định user" });
            }

            var stats = await _service.GetUserStatsAsync(userId, leaderboardId);
            if (stats == null) return NotFound(new { message = "Không tìm thấy thông tin mùa giải" });
            
            return Ok(stats);
        }

        /// <summary>
        /// Lấy thống kê của user khác trong mùa giải
        /// </summary>
        [HttpGet("user/{userId:int}/stats")]
        public async Task<ActionResult<UserSeasonStatsDTO>> GetUserStats(
            int userId, 
            [FromQuery] int? leaderboardId = null)
        {
            var stats = await _service.GetUserStatsAsync(userId, leaderboardId);
            if (stats == null) return NotFound();
            return Ok(stats);
        }

        /// <summary>
        /// Lấy thông tin tính điểm TOEIC của user
        /// </summary>
        [Authorize]
        [HttpGet("user/toeic-calculation")]
        public async Task<ActionResult<TOEICScoreCalculationDTO>> GetMyTOEICCalculation(
            [FromQuery] int? leaderboardId = null)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var calculation = await _service.GetUserTOEICCalculationAsync(userId, leaderboardId);
            if (calculation == null) return NotFound();
            return Ok(calculation);
        }

        /// <summary>
        /// Lấy thứ hạng của user trong mùa giải
        /// </summary>
        [Authorize]
        [HttpGet("user/rank")]
        public async Task<ActionResult<int>> GetMyRank([FromQuery] int? leaderboardId = null)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var rank = await _service.GetUserRankAsync(userId, leaderboardId);
            return Ok(new { rank });
        }

        /// <summary>
        /// Tự động quản lý mùa giải (kích hoạt và kết thúc) (Chỉ Staff)
        /// Endpoint này nên được gọi định kỳ bởi background job
        /// </summary>
        [Authorize(Roles = "Staff")]
        [HttpPost("auto-manage")]
        public async Task<ActionResult> AutoManageSeasons()
        {
            await _service.AutoManageSeasonsAsync();
            return Ok(new { message = "Đã tự động quản lý mùa giải" });
        }
    }
}
