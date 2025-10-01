using DataLayer.DTOs.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace lumina.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
            private readonly IUserService _userService;

            public UserController(IUserService userService)
            {
                _userService = userService;
            }
        [HttpGet("non-admin-users")]
        public async Task<IActionResult> GetFilteredUsersPaged([FromQuery] int pageNumber = 1, [FromQuery] string? searchTerm = null, [FromQuery] string? roleName = null)
        {
            int pageSize = 6;
            var (users, totalPages) = await _userService.GetFilteredUsersPagedAsync(pageNumber, pageSize, searchTerm, roleName);
            return Ok(new { data = users, totalPages = totalPages });
        }

        // GET: api/User/123
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return Ok(user);
        }

        // PATCH: api/User/123/toggle-status
        [HttpPatch("{id}/toggle-status")]
        public async Task<IActionResult> ToggleUserStatus(int id)
        {
            var success = await _userService.ToggleUserStatusAsync(id);
            if (!success)
            {
                return NotFound(new { message = "User not found." });
            }
            return NoContent(); 
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(idClaim) || !int.TryParse(idClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid token" });
            }
            var dto = await _userService.GetCurrentUserProfileAsync(userId);
            return dto is null ? NotFound() : Ok(dto);
        }

        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserDTO request)
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(idClaim) || !int.TryParse(idClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid token" });
            }
            var dto = await _userService.UpdateCurrentUserProfileAsync(userId, request);
            return dto is null ? NotFound() : Ok(dto);
        }

        [HttpPut("profile/change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO request)
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(idClaim) || !int.TryParse(idClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            try
            {
                await _userService.ChangePasswordAsync(userId, request);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

    }
}
