using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Speech;
using ServiceLayer.TextToSpeech;
using ServiceLayer.UploadFile;
using Microsoft.Extensions.Configuration;

namespace lumina.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        private readonly IUploadService _uploadService;

        private readonly ITextToSpeechService _textToSpeechService;

        public UploadController(IUploadService uploadService, ITextToSpeechService textToSpeechService)
        {
            _uploadService = uploadService;
            _textToSpeechService = textToSpeechService;
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Không có file nào được chọn." });
            }

            try
            {
                var uploadResult = await _uploadService.UploadFileAsync(file);
                return Ok(uploadResult);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log chi tiết lỗi để debug
                Console.WriteLine($"Error uploading file: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return StatusCode(500, new { message = $"Lỗi server nội bộ: {ex.Message}", details = ex.StackTrace });
            }
        }

        [HttpPost("url")]
        public async Task<IActionResult> UploadFromUrl([FromBody] UploadFromUrlRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.FileUrl))
                return BadRequest("URL không hợp lệ.");

            try
            {
                var uploadResult = await _uploadService.UploadFromUrlAsync(request.FileUrl);
                return Ok(uploadResult);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server nội bộ: {ex.Message}");
            }
        }


          [HttpPost("upload")]
        public async Task<IActionResult> GenerateQuestionAudioAsync([FromBody] TextToSpeechRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.Text))
                {
                    return BadRequest("Text không được để trống");
                }

                var uploadResult = await _textToSpeechService.GenerateAudioAsync(
                    request.Text
                );

                return Ok(uploadResult);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

    }


}
    public class UploadFromUrlRequest
    {
        public string FileUrl { get; set; } 
    }

    public class TextToSpeechRequest
    {
        public string Text { get; set; }
    }

