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

        /// <summary>
        /// Save writing answer to database
        /// </summary>
        /// <param name="request">Writing answer request DTO</param>
        /// <returns>Success status</returns>
        [HttpPost("save-answer")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SaveWritingAnswer([FromBody] WritingAnswerRequestDTO request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (request.AttemptID <= 0)
                {
                    return BadRequest(new { Message = "Invalid AttemptID." });
                }

                if (request.QuestionId <= 0)
                {
                    return BadRequest(new { Message = "Invalid QuestionId." });
                }

                if (string.IsNullOrWhiteSpace(request.UserAnswerContent))
                {
                    return BadRequest(new { Message = "UserAnswerContent cannot be empty." });
                }

                var result = await _writingService.SaveWritingAnswer(request);

                if (result)
                {
                    return Ok(new { Message = "Writing answer saved successfully.", Success = true });
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, 
                        new { Message = "Failed to save writing answer.", Success = false });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while saving writing answer for AttemptID {AttemptID}, QuestionId {QuestionId}", 
                    request?.AttemptID, request?.QuestionId);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { Message = "An unexpected error occurred while saving writing answer.", Success = false });
            }
        }

        /// <summary>
        /// Get AI feedback for writing answer
        /// </summary>
        /// <param name="request">Writing request DTO with picture caption and user answer</param>
        /// <returns>AI feedback with score and detailed evaluation</returns>
        [HttpPost("get-feedback")]
        [ProducesResponseType(typeof(WritingResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetFeedbackFromAI([FromBody] WritingRequestDTO request)
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

                var result = await _writingService.GetFeedbackFromAI(request);

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
