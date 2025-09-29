using Microsoft.AspNetCore.Mvc;

namespace lumina.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoleController : ControllerBase
    {
        private readonly IRoleService _roleService;

        public RoleController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllRoles()
        {
            try
            {
                var roles = await _roleService.GetAllRolesAsync();
                return Ok(roles);
            }
            catch (System.Exception ex)
            {
                // Ghi lại lỗi (log) ở đây
                return StatusCode(500, "Lỗi máy chủ nội bộ");
            }
        }
    }
}
