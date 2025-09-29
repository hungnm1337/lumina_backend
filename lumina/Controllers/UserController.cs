using Microsoft.AspNetCore.Mvc;
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

    }
}
