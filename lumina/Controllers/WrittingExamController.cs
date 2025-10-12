using DataLayer.DTOs.Exam.Writting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Exam.Writting;

namespace lumina.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WrittingExamController : ControllerBase
    {
        public readonly IWrittingService _writtingService;

        public WrittingExamController(IWrittingService writtingService)
        {
            _writtingService = writtingService;
        }

        [HttpPost("feedback")]
        public async Task<ActionResult<WrittingResponseDTO>> GetFeedbackFromAI([FromBody] WrittingRequestDTO request)
        {
            if (request == null || string.IsNullOrEmpty(request.PictureCaption) || string.IsNullOrEmpty(request.VocabularyRequest) || string.IsNullOrEmpty(request.UserAnswer))
            {
                return BadRequest("Invalid request data.");
            }
            try
            {
                var response = await _writtingService.GetFeedbackFromAI(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                // Log the exception (ex) here as needed
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
