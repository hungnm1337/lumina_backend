using DataLayer.DTOs.Exam.Writting;
using DataLayer.DTOs.UserAnswer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ServiceLayer.Exam.Writting;
using System;
using System.Threading.Tasks;

namespace lumina.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WritingController : ControllerBase
    {
        private readonly IWritingService _writingService;
        private readonly ILogger<WritingController> _logger;

        public WritingController(IWritingService writingService, ILogger<WritingController> logger)
        {
            _writingService = writingService;
            _logger = logger;
        }

        
        [HttpPost("save-answer")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SaveWritingAnswer([FromBody] WritingAnswerRequestDTO request)
        {
            try
            {
                if (request == null) return BadRequest(new { Message = "Request cannot be null." });
                if (!ModelState.IsValid) return BadRequest(ModelState);
                if (request.AttemptID <= 0) return BadRequest(new { Message = "Invalid AttemptID." });
                if (request.QuestionId <= 0) return BadRequest(new { Message = "Invalid QuestionId." });
                if (string.IsNullOrWhiteSpace(request.UserAnswerContent)) 
                    return BadRequest(new { Message = "UserAnswerContent cannot be empty." });

                var result = await _writingService.SaveWritingAnswer(request);

                return result 
                    ? Ok(new { Message = "Writing answer saved successfully.", Success = true })
                    : StatusCode(StatusCodes.Status500InternalServerError, 
                        new { Message = "Failed to save writing answer.", Success = false });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while saving writing answer for AttemptID {AttemptID}, QuestionId {QuestionId}", 
                    request?.AttemptID, request?.QuestionId);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { Message = "An unexpected error occurred while saving writing answer.", Success = false });
            }
        }

        
        [HttpPost("p1-get-feedback")]
        [ProducesResponseType(typeof(WritingResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetFeedbackP1FromAI([FromBody] WritingRequestP1DTO request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (string.IsNullOrWhiteSpace(request.UserAnswer))
                {
                    return BadRequest(new { Message = "UserAnswer cannot be empty." });
                }

                if (string.IsNullOrWhiteSpace(request.PictureCaption))
                {
                    return BadRequest(new { Message = "PictureCaption cannot be empty." });
                }

                var result = await _writingService.GetFeedbackP1FromAI(request);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting AI feedback for writing");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { Message = "An unexpected error occurred while getting AI feedback." });
            }
        }

        [HttpPost("p23-get-feedback")]
        [ProducesResponseType(typeof(WritingResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetFeedbackP23FromAI([FromBody] WritingRequestP23DTO request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (string.IsNullOrWhiteSpace(request.UserAnswer))
                {
                    return BadRequest(new { Message = "UserAnswer cannot be empty." });
                }

                if (string.IsNullOrWhiteSpace(request.Prompt))
                {
                    return BadRequest(new { Message = "Prompt cannot be empty." });
                }

                var result = await _writingService.GetFeedbackP23FromAI(request);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting AI feedback for writing");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Message = "An unexpected error occurred while getting AI feedback." });
            }
        }

    }

   
}
