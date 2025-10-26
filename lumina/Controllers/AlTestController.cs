using DataLayer.DTOs;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.ExamGenerationAI;
using ServiceLayer.TextToSpeech;

namespace lumina.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AITestController : ControllerBase
    {
        private readonly IExamGenerationAIService _gemini;

        private readonly ITextToSpeechService _textToSpeechService;

        public AITestController(IExamGenerationAIService gemini, ITextToSpeechService textToSpeechService)
        {
            _gemini = gemini;
            _textToSpeechService = textToSpeechService;
        }

        [HttpPost("test")]
        public async Task<IActionResult> TestPrompt([FromBody] string prompt)
        {
            var result = await _gemini.GenerateResponseAsync(prompt);
            return Ok(result);
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
