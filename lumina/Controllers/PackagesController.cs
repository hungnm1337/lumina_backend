using DataLayer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Packages;

namespace lumina.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PackagesController : ControllerBase
    {
        private readonly IPackageService _packageService;

        public PackagesController(IPackageService packageService)
        {
            _packageService = packageService;
        }

        [HttpGet("active")]
        [AllowAnonymous]
        public async Task<IActionResult> GetActivePackages()
        {
            var list = await _packageService.GetActivePackagesAsync();
            return Ok(list);
        }

        [HttpGet("active-pro")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetActiveProPackages()
        {
            var list = await _packageService.GetActivePackagesAsync();
            return Ok(list);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPackage(int id)
        {
            var pkg = await _packageService.GetByIdAsync(id);
            if (pkg == null) return NotFound();
            return Ok(pkg);
        }

       /* [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreatePackage([FromBody] Package pkg)
        {
            await _packageService.AddPackageAsync(pkg);
            return CreatedAtAction(nameof(GetPackage), new { id = pkg.PackageId }, pkg);
        }*/


        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdatePackage(int id, [FromBody] Package updatedPackage)
        {
            if (id != updatedPackage.PackageId)
                return BadRequest("Package ID mismatch");

            var existingPkg = await _packageService.GetByIdAsync(id);
            if (existingPkg == null) return NotFound();

            // Cập nhật các trường cần thiết
            existingPkg.PackageName = updatedPackage.PackageName;
            existingPkg.Price = updatedPackage.Price;
            existingPkg.DurationInDays = updatedPackage.DurationInDays;
            existingPkg.IsActive = updatedPackage.IsActive;

            await _packageService.UpdatePackageAsync(existingPkg);
            return NoContent();
        }

        [HttpPatch("{id}/toggle-status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            await _packageService.TogglePackageStatusAsync(id);
            return NoContent();
        }
/*
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePackage(int id)
        {
            await _packageService.DeletePackageAsync(id);
            return NoContent();
        }*/

        [HttpGet("user-active-package/{userId}")]
        public async Task<IActionResult> GetUserActivePackage(int userId)
        {
            var package = await _packageService.GetUserActivePackageAsync(userId);
            if (package == null)
                return NotFound("User has no active package.");

            return Ok(package);
        }
    }

}
