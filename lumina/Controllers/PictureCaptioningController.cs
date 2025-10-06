using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;


namespace lumina.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PictureCaptioningController : ControllerBase
    {
        private readonly ImageCaptioningService _captioningService;

        public PictureCaptioningController(ImageCaptioningService captioningService)
        {
            _captioningService = captioningService;
        }

        [HttpPost("generate-caption")]
        public async Task<IActionResult> GenerateCaption([FromBody] string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
            {
                return BadRequest("Image URL is required.");
            }

            // Gọi API Python
            string caption = await _captioningService.GetCaptionFromImageUrl(imageUrl);

            if (caption.StartsWith("Error"))
            {
                // Trả về lỗi nếu có vấn đề (Ví dụ: 500 Internal Server Error)
                return StatusCode(500, new { error = caption });
            }

            // Trả về kết quả thành công
            return Ok(new { imageUrl = imageUrl, caption = caption });
        }
    }
}
