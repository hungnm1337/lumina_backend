using DataLayer.DTOs.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.User;
using System.Security.Claims;

namespace lumina.Controllers;


[Route("api/[controller]")]
[ApiController]
public sealed class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    
    [HttpGet("non-admin-users")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetFilteredUsersPaged(
        [FromQuery] int pageNumber = 1,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? roleName = null)
    {
        const int pageSize = 6;
        var (users, totalPages) = await _userService.GetFilteredUsersPagedAsync(pageNumber, pageSize, searchTerm, roleName);
        return Ok(new { data = users, totalPages });
    }

    
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUserById(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);

        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        return Ok(user);
    }

    [HttpPatch("{id}/toggle-status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ToggleUserStatus(int id)
    {
        var success = await _userService.ToggleUserStatusAsync(id);

        if (!success)
        {
            return NotFound(new { message = "User not found" });
        }

        return NoContent();
    }



    
    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        // Lấy userId từ JWT token claim (NameIdentifier)
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "Invalid token" });
        }

        var profile = await _userService.GetCurrentUserProfileAsync(userId.Value);

        if (profile == null)
        {
            return NotFound(new { message = "Profile not found" });
        }

        return Ok(profile);
    }

    
    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserProfileDTO request)
    {
        // Model validation được xử lý tự động bởi ASP.NET Core
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return BadRequest(new { message = "Validation failed", errors });
        }

        // Lấy userId từ JWT token claim (NameIdentifier)
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "Invalid token" });
        }

        try
        {
            var updatedProfile = await _userService.UpdateCurrentUserProfileAsync(userId.Value, request);

            if (updatedProfile == null)
            {
                return NotFound(new { message = "Profile not found" });
            }

            return Ok(updatedProfile);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    
    [HttpPut("profile/change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Lấy userId từ JWT token claim (NameIdentifier)
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "Invalid token" });
        }

        try
        {
            await _userService.ChangePasswordAsync(userId.Value, request);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }



    
    private int? GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(idClaim) || !int.TryParse(idClaim, out var userId))
        {
            return null;
        }

        return userId;
    }


    [HttpPut("update-role")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateUserRole(int userId, int roleId)
    {
        var result = await _userService.UpdateUserRoleAsync(userId, roleId);
        if (result)
            return Ok(new { message = "Cập nhật quyền thành công!" });
        return NotFound(new { message = "Không tìm thấy user hoặc cập nhật thất bại!" });
    }

}
