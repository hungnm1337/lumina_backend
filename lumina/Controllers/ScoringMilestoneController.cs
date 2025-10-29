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
    public class ScoringMilestoneController : ControllerBase
    {
        private readonly IScoringMilestoneService _service;

        public ScoringMilestoneController(IScoringMilestoneService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<ScoringMilestoneDTO>>> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{milestoneId:int}")]
        public async Task<ActionResult<ScoringMilestoneDTO>> GetById(int milestoneId)
        {
            var result = await _service.GetByIdAsync(milestoneId);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<int>> Create([FromBody] CreateScoringMilestoneDTO dto)
        {
            var id = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { milestoneId = id }, id);
        }

        [Authorize]
        [HttpPut("{milestoneId:int}")]
        public async Task<ActionResult> Update(int milestoneId, [FromBody] UpdateScoringMilestoneDTO dto)
        {
            var success = await _service.UpdateAsync(milestoneId, dto);
            if (!success) return NotFound();
            return NoContent();
        }

        [Authorize]
        [HttpDelete("{milestoneId:int}")]
        public async Task<ActionResult> Delete(int milestoneId)
        {
            var success = await _service.DeleteAsync(milestoneId);
            if (!success) return NotFound();
            return NoContent();
        }

        [HttpGet("user/{userId:int}/notifications")]
        public async Task<ActionResult<List<UserMilestoneNotificationDTO>>> GetUserNotifications(int userId)
        {
            var result = await _service.GetUserNotificationsAsync(userId);
            return Ok(result);
        }

        [HttpPost("notifications/{notificationId:int}/read")]
        public async Task<ActionResult> MarkAsRead(int notificationId)
        {
            var success = await _service.MarkNotificationAsReadAsync(notificationId);
            if (!success) return NotFound();
            return NoContent();
        }

        [HttpPost("check-milestones/{userId:int}")]
        public async Task<ActionResult> CheckMilestones(int userId, [FromBody] int currentScore)
        {
            await _service.CheckAndCreateMilestoneNotificationsAsync(userId, currentScore);
            return Ok();
        }

        [Authorize]
        [HttpPost("initialize-defaults")]
        public async Task<ActionResult> InitializeDefaults()
        {
            await _service.InitializeDefaultMilestonesAsync();
            return Ok();
        }
    }
}


