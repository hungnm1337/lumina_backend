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

        
        [HttpGet]
        public async Task<ActionResult<PaginatedResultDTO<LeaderboardDTO>>> GetAll(
            [FromQuery] string? keyword = null, 
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetAllPaginatedAsync(keyword, page, pageSize);
            return Ok(result);
        }

        
        [HttpGet("all")]
        public async Task<ActionResult<List<LeaderboardDTO>>> GetAllSimple([FromQuery] bool? isActive = null)
        {
            var result = await _service.GetAllAsync(isActive);
            return Ok(result);
        }

        
        [HttpGet("current")]
        public async Task<ActionResult<LeaderboardDTO?>> GetCurrent()
        {
            var dto = await _service.GetCurrentAsync();
            if (dto == null) return NotFound(new { message = "Không có mùa giải nào đang diễn ra" });
            return Ok(dto);
        }

        
        [HttpGet("{leaderboardId:int}")]
        public async Task<ActionResult<LeaderboardDTO>> GetById(int leaderboardId)
        {
            var dto = await _service.GetByIdAsync(leaderboardId);
            if (dto == null) return NotFound();
            return Ok(dto);
        }

        
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

       
        [Authorize(Roles = "Staff")]
        [HttpDelete("{leaderboardId:int}")]
        public async Task<ActionResult> Delete(int leaderboardId)
        {
            var ok = await _service.DeleteAsync(leaderboardId);
            if (!ok) return NotFound();
            return NoContent();
        }

        
        [Authorize(Roles = "Staff")]
        [HttpPost("{leaderboardId:int}/set-current")]
        public async Task<ActionResult> SetCurrent(int leaderboardId)
        {
            var ok = await _service.SetCurrentAsync(leaderboardId);
            if (!ok) return NotFound();
            return NoContent();
        }

        
        [HttpGet("{leaderboardId:int}/ranking")]
        public async Task<ActionResult<List<LeaderboardRankDTO>>> GetRanking(
            int leaderboardId, 
            [FromQuery] int top = 100)
        {
            var result = await _service.GetSeasonRankingAsync(leaderboardId, top);
            return Ok(result);
        }

       
        [Authorize(Roles = "Staff")]
        [HttpPost("{leaderboardId:int}/recalculate")]
        public async Task<ActionResult<int>> Recalculate(int leaderboardId)
        {
            var affected = await _service.RecalculateSeasonScoresAsync(leaderboardId);
            return Ok(new { affected, message = $"Đã tính lại điểm cho {affected} người dùng" });
        }

        
        [Authorize(Roles = "Staff")]
        [HttpPost("{leaderboardId:int}/reset")]
        public async Task<ActionResult<int>> ResetSeason(
            int leaderboardId, 
            [FromQuery] bool archiveScores = true)
        {
            var affected = await _service.ResetSeasonAsync(leaderboardId, archiveScores);
            return Ok(new { affected, message = $"Đã reset điểm cho mùa giải. {affected} bản ghi bị xóa" });
        }

       
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

        
        [HttpGet("user/{userId:int}/stats")]
        public async Task<ActionResult<UserSeasonStatsDTO>> GetUserStats(
            int userId, 
            [FromQuery] int? leaderboardId = null)
        {
            var stats = await _service.GetUserStatsAsync(userId, leaderboardId);
            if (stats == null) return NotFound();
            return Ok(stats);
        }

       
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

       
        [Authorize]
        [HttpPost("calculate-score")]
        public async Task<ActionResult<CalculateScoreResponseDTO>> CalculateScore([FromBody] CalculateScoreRequestDTO request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Không thể xác định user" });
            }

            try
            {
                Console.WriteLine($" [LeaderboardController] Calculate score request:");
                Console.WriteLine($"   UserId: {userId}");
                Console.WriteLine($"   ExamAttemptId: {request.ExamAttemptId}");
                Console.WriteLine($"   ExamPartId: {request.ExamPartId}");
                Console.WriteLine($"   CorrectAnswers: {request.CorrectAnswers}/{request.TotalQuestions}");
                
                var result = await _service.CalculateSeasonScoreAsync(userId, request);
                
                Console.WriteLine($" [LeaderboardController] Score calculated:");
                Console.WriteLine($"   SeasonScore: {result.SeasonScore}");
                Console.WriteLine($"   TotalAccumulatedScore: {result.TotalAccumulatedScore}");
                Console.WriteLine($"   EstimatedTOEIC: {result.EstimatedTOEIC}");
                Console.WriteLine($"   IsFirstAttempt: {result.IsFirstAttempt}");
                
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($" [LeaderboardController] ArgumentException: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($" [LeaderboardController] Exception: {ex.Message}");
                Console.WriteLine($"   StackTrace: {ex.StackTrace}");
                return StatusCode(500, new { message = "Lỗi khi tính điểm: " + ex.Message });
            }
        }

        [Authorize(Roles = "Staff")]
        [HttpPost("auto-manage")]
        public async Task<ActionResult> AutoManageSeasons()
        {
            await _service.AutoManageSeasonsAsync();
            return Ok(new { message = "Đã tự động quản lý mùa giải" });
        }
    }
}
