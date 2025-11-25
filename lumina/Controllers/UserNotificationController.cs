using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Notification;
using DataLayer.DTOs.Notification;
using System.Security.Claims;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace lumina.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserNotificationController : ControllerBase
    {
        private readonly IUserNotificationService _userNotificationService;

        public UserNotificationController(IUserNotificationService userNotificationService)
        {
            _userNotificationService = userNotificationService;
        }

        [HttpGet]
        public async Task<ActionResult<List<UserNotificationDTO>>> GetMyNotifications()
        {
            int userId = GetUserId();
            var notifications = await _userNotificationService.GetByUserIdAsync(userId);
            return Ok(notifications);
        }

        [HttpGet("unread-count")]
        public async Task<ActionResult<int>> GetUnreadCount()
        {
            int userId = GetUserId();
            var count = await _userNotificationService.GetUnreadCountAsync(userId);
            return Ok(new { unreadCount = count });
        }

        [HttpGet("{uniqueId:int}")]
        public async Task<ActionResult<UserNotificationDTO>> GetById(int uniqueId)
        {
            int userId = GetUserId();
            var notification = await _userNotificationService.GetByIdAsync(uniqueId);
            
            if (notification == null)
            {
                return NotFound(new { message = "Notification not found" });
            }

            if (notification.UserId != userId)
            {
                return Forbid();
            }

            return Ok(notification);
        }

        [HttpPut("{uniqueId:int}/read")]
        public async Task<ActionResult> MarkAsRead(int uniqueId)
        {
            int userId = GetUserId();
            var ok = await _userNotificationService.MarkAsReadAsync(uniqueId, userId);
            
            if (!ok)
            {
                return NotFound(new { message = "Notification not found or unauthorized" });
            }

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
