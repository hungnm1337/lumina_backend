using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Leaderboard;
using DataLayer.DTOs.Leaderboard;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace lumina.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ScoringRuleController : ControllerBase
    {
        private readonly IScoringRuleService _service;

        public ScoringRuleController(IScoringRuleService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<ScoringRuleDTO>>> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{ruleId:int}")]
        public async Task<ActionResult<ScoringRuleDTO>> GetById(int ruleId)
        {
            var result = await _service.GetByIdAsync(ruleId);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<int>> Create([FromBody] CreateScoringRuleDTO dto)
        {
            var id = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { ruleId = id }, id);
        }

        [Authorize]
        [HttpPut("{ruleId:int}")]
        public async Task<ActionResult> Update(int ruleId, [FromBody] UpdateScoringRuleDTO dto)
        {
            var success = await _service.UpdateAsync(ruleId, dto);
            if (!success) return NotFound();
            return NoContent();
        }

        [Authorize]
        [HttpDelete("{ruleId:int}")]
        public async Task<ActionResult> Delete(int ruleId)
        {
            var success = await _service.DeleteAsync(ruleId);
            if (!success) return NotFound();
            return NoContent();
        }

        [HttpPost("calculate/{userId:int}")]
        public async Task<ActionResult<int>> CalculateScore(int userId, [FromBody] CalculateScoreRequest request)
        {
            var score = await _service.CalculateSessionScoreAsync(
                userId, 
                request.TotalQuestions, 
                request.CorrectAnswers, 
                request.TimeSpentSeconds, 
                request.Difficulty ?? "medium"
            );
            return Ok(score);
        }

        [HttpGet("user/{userId:int}/scores")]
        public async Task<ActionResult<List<PracticeSessionScoreDTO>>> GetUserScores(int userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetUserSessionScoresAsync(userId, page, pageSize);
            return Ok(result);
        }

        [Authorize]
        [HttpPost("initialize-defaults")]
        public async Task<ActionResult> InitializeDefaults()
        {
            await _service.InitializeDefaultScoringRulesAsync();
            return Ok();
        }
    }

    public class CalculateScoreRequest
    {
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public int TimeSpentSeconds { get; set; }
        public string? Difficulty { get; set; }
    }
}


