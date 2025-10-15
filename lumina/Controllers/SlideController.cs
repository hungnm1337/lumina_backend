using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Slide;
using DataLayer.DTOs;
using System.Security.Claims;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace lumina.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SlideController : ControllerBase
    {
        private readonly ISlideService _slideService;

        public SlideController(ISlideService slideService)
        {
            _slideService = slideService;
        }

        [HttpGet]
        public async Task<ActionResult<List<SlideDTO>>> GetAll([FromQuery] string? keyword = null, [FromQuery] bool? isActive = null)
        {
            var items = await _slideService.GetAllAsync(keyword, isActive);
            return Ok(items);
        }

        [HttpGet("{slideId:int}")]
        public async Task<ActionResult<SlideDTO>> GetById(int slideId)
        {
            var item = await _slideService.GetByIdAsync(slideId);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<int>> Create([FromBody] SlideDTO dto)
        {
            int userId = GetUserId();
            // Ensure server controls audit fields
            dto.CreateBy = userId;
            var id = await _slideService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { slideId = id }, id);
        }

        [Authorize]
        [HttpPut("{slideId:int}")]
        public async Task<ActionResult> Update(int slideId, [FromBody] SlideDTO dto)
        {
            if(slideId != dto.SlideId)
            {
                return BadRequest("Slide ID mismatch");
            }
            int userId = GetUserId();
            // Ensure server controls audit fields
            dto.UpdateBy = userId;
            var ok = await _slideService.UpdateAsync(dto);
            if (!ok) return NotFound();
            return NoContent();
        }

        [Authorize]
        [HttpDelete("{slideId:int}")]
        public async Task<ActionResult> Delete(int slideId)
        {
            var ok = await _slideService.DeleteAsync(slideId);
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