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

       

        [HttpGet("generate-caption")]
        public async Task<IActionResult> GenerateCaption([FromQuery] string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
            {
                return BadRequest("Image URL is required.");
            }

            string caption = await _captioningService.GetCaptionFromImageUrl(imageUrl);

            if (caption.StartsWith("Error"))
            {
                return StatusCode(500, new { error = caption });
            }

            return Ok(new { caption = caption });
        }
    }
}
