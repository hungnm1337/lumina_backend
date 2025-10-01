using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Event;
using DataLayer.DTOs;
using System.Security.Claims;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace lumina.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventController : ControllerBase
    {
        private readonly IEventService _eventService;

        public EventController(IEventService eventService)
        {
            _eventService = eventService;
        }

        [HttpGet]
        public async Task<ActionResult<List<EventDTO>>> GetAll([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null, [FromQuery] string? keyword = null)
        {
            var items = await _eventService.GetAllAsync(from, to, keyword);
            return Ok(items);
        }

        [HttpGet("{eventId:int}")]
        public async Task<ActionResult<EventDTO>> GetById(int eventId)
        {
            var item = await _eventService.GetByIdAsync(eventId);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<int>> Create([FromBody] EventDTO dto)
        {
            int userId = GetUserId();
            var id = await _eventService.CreateAsync(dto, userId);
            return CreatedAtAction(nameof(GetById), new { eventId = id }, id);
        }

        [Authorize]
        [HttpPut("{eventId:int}")]
        public async Task<ActionResult> Update(int eventId, [FromBody] EventDTO dto)
        {
            int userId = GetUserId();
            var ok = await _eventService.UpdateAsync(eventId, dto, userId);
            if (!ok) return NotFound();
            return NoContent();
        }

        [Authorize]
        [HttpDelete("{eventId:int}")]
        public async Task<ActionResult> Delete(int eventId)
        {
            var ok = await _eventService.DeleteAsync(eventId);
            if (!ok) return NotFound();
            return NoContent();
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                throw new UnauthorizedAccessException("Missing user id claim");
            }
            return int.Parse(userIdClaim);
        }
    }
}
