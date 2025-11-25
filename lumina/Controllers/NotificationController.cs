using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Notification;
using DataLayer.DTOs;
using DataLayer.DTOs.Notification;
using System.Security.Claims;
using System;
using System.Threading.Tasks;

namespace lumina.Controllers
{
    [Route("api/admin/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<ActionResult<PaginatedResultDTO<NotificationDTO>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _notificationService.GetAllPaginatedAsync(page, pageSize);
            return Ok(result);
        }

        [HttpGet("{notificationId:int}")]
        public async Task<ActionResult<NotificationDTO>> GetById(int notificationId)
        {
            var item = await _notificationService.GetByIdAsync(notificationId);
            if (item == null) return NotFound(new { message = "Notification not found" });
            return Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<int>> Create([FromBody] CreateNotificationDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            int userId = GetUserId();
            var id = await _notificationService.CreateAsync(dto, userId);
            return CreatedAtAction(nameof(GetById), new { notificationId = id }, new { notificationId = id });
        }

        [HttpPut("{notificationId:int}")]
        public async Task<ActionResult> Update(int notificationId, [FromBody] UpdateNotificationDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var ok = await _notificationService.UpdateAsync(notificationId, dto);
            if (!ok) return NotFound(new { message = "Notification not found" });
            return NoContent();
        }

        [HttpDelete("{notificationId:int}")]
        public async Task<ActionResult> Delete(int notificationId)
        {
            var ok = await _notificationService.DeleteAsync(notificationId);
            if (!ok) return NotFound(new { message = "Notification not found" });
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
