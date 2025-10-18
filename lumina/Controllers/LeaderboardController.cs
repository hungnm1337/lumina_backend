using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Leaderboard;
using DataLayer.DTOs;
using DataLayer.DTOs.Leaderboard;
using System;
using System.Collections.Generic;
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
        public async Task<ActionResult<PaginatedResultDTO<LeaderboardDTO>>> GetAll([FromQuery] string? keyword = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
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
            if (dto == null) return NotFound();
            return Ok(dto);
        }

        [HttpGet("{leaderboardId:int}")]
        public async Task<ActionResult<LeaderboardDTO>> GetById(int leaderboardId)
        {
            var dto = await _service.GetByIdAsync(leaderboardId);
            if (dto == null) return NotFound();
            return Ok(dto);
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<int>> Create([FromBody] CreateLeaderboardDTO dto)
        {
            var id = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { leaderboardId = id }, id);
        }

        [Authorize]
        [HttpPut("{leaderboardId:int}")]
        public async Task<ActionResult> Update(int leaderboardId, [FromBody] UpdateLeaderboardDTO dto)
        {
            var ok = await _service.UpdateAsync(leaderboardId, dto);
            if (!ok) return NotFound();
            return NoContent();
        }

        [Authorize]
        [HttpDelete("{leaderboardId:int}")]
        public async Task<ActionResult> Delete(int leaderboardId)
        {
            var ok = await _service.DeleteAsync(leaderboardId);
            if (!ok) return NotFound();
            return NoContent();
        }

        [Authorize]
        [HttpPost("{leaderboardId:int}/set-current")]
        public async Task<ActionResult> SetCurrent(int leaderboardId)
        {
            var ok = await _service.SetCurrentAsync(leaderboardId);
            if (!ok) return NotFound();
            return NoContent();
        }

        [HttpGet("{leaderboardId:int}/ranking")]
        public async Task<ActionResult<List<LeaderboardRankDTO>>> GetRanking(int leaderboardId, [FromQuery] int top = 100)
        {
            var result = await _service.GetSeasonRankingAsync(leaderboardId, top);
            return Ok(result);
        }

        [Authorize]
        [HttpPost("{leaderboardId:int}/recalculate")]
        public async Task<ActionResult<int>> Recalculate(int leaderboardId)
        {
            var affected = await _service.RecalculateSeasonScoresAsync(leaderboardId);
            return Ok(affected);
        }
    }
}
