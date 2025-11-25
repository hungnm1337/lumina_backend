using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Slide;
using ServiceLayer.UploadFile;
using DataLayer.DTOs;
using System.Security.Claims;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace lumina.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SlideController : ControllerBase
    {
        private readonly ISlideService _slideService;
        private readonly IUploadService _uploadService;

        public SlideController(ISlideService slideService, IUploadService uploadService)
        {
            _slideService = slideService;
            _uploadService = uploadService;
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
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<int>> Create([FromForm] string slideName, [FromForm] bool? isActive, [FromForm] IFormFile? imageFile)
        {
            int userId = GetUserId();
            
            if (string.IsNullOrEmpty(slideName))
            {
                return BadRequest("Tên slide không được để trống.");
            }

            string slideUrl = string.Empty;

            // Nếu có file ảnh, upload lên Cloudinary
            if (imageFile != null && imageFile.Length > 0)
            {
                try
                {
                    var uploadResult = await _uploadService.UploadFileAsync(imageFile);
                    slideUrl = uploadResult.Url;
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Lỗi khi upload ảnh: {ex.Message}");
                }
            }
            else
            {
                return BadRequest("Vui lòng chọn ảnh để upload.");
            }

            var dto = new SlideDTO
            {
                SlideName = slideName,
                SlideUrl = slideUrl,
                IsActive = isActive ?? true,
                CreateBy = userId,
                CreateAt = DateTime.UtcNow
            };

            var id = await _slideService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { slideId = id }, id);
        }

        [Authorize]
        [HttpPut("{slideId:int}")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult> Update(int slideId, [FromForm] string slideName, [FromForm] bool? isActive, [FromForm] IFormFile? imageFile)
        {
            int userId = GetUserId();
            
            // Lấy slide hiện tại
            var existingSlide = await _slideService.GetByIdAsync(slideId);
            if (existingSlide == null)
            {
                return NotFound();
            }

            if (string.IsNullOrEmpty(slideName))
            {
                return BadRequest("Tên slide không được để trống.");
            }

            string slideUrl = existingSlide.SlideUrl;

            // Nếu có file ảnh mới, upload lên Cloudinary
            if (imageFile != null && imageFile.Length > 0)
            {
                try
                {
                    var uploadResult = await _uploadService.UploadFileAsync(imageFile);
                    slideUrl = uploadResult.Url;
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Lỗi khi upload ảnh: {ex.Message}");
                }
            }

            var dto = new SlideDTO
            {
                SlideId = slideId,
                SlideName = slideName,
                SlideUrl = slideUrl,
                IsActive = isActive ?? true,
                UpdateBy = userId,
                UpdateAt = DateTime.UtcNow,
                CreateBy = existingSlide.CreateBy,
                CreateAt = existingSlide.CreateAt
            };

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