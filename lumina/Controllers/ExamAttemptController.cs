using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Exam.ExamAttempt;
using DataLayer.DTOs.UserAnswer;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace lumina.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExamAttemptController : ControllerBase
    {
        private readonly IExamAttemptService _examAttemptService;
        private readonly ILogger<ExamAttemptController> _logger;

        public ExamAttemptController(
            IExamAttemptService examAttemptService,
            ILogger<ExamAttemptController> logger)
        {
            _examAttemptService = examAttemptService;
            _logger = logger;
        }

        [HttpPost("start-exam")]
        [ProducesResponseType(typeof(ExamAttemptResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> StartAnExam([FromBody] ExamAttemptRequestDTO model)
        {
            try
            {
                if (model == null)
                {
                    return BadRequest(new { Message = "Request body cannot be null." });
                }

                if (model.UserID <= 0)
                {
                    return BadRequest(new { Message = "Invalid UserID." });
                }

                if (model.ExamID <= 0)
                {
                    return BadRequest(new { Message = "Invalid ExamID." });
                }

                var result = await _examAttemptService.StartAnExam(model);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while starting exam for UserID {UserID}, ExamID {ExamID}", model?.UserID, model?.ExamID);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while starting the exam.");
            }
        }

        [HttpPatch("end-exam")]
        [ProducesResponseType(typeof(ExamAttemptResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> EndAnExam([FromBody] ExamAttemptRequestDTO model)
        {
            try
            {
                if (model == null)
                {
                    return BadRequest(new { Message = "Request body cannot be null." });
                }

                if (model.AttemptID <= 0)
                {
                    return BadRequest(new { Message = "Invalid AttemptID." });
                }

                var result = await _examAttemptService.EndAnExam(model);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Attempted to end a non-existent exam attempt. ID: {AttemptID}", model?.AttemptID);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while ending exam attempt {AttemptID}", model?.AttemptID);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while ending the exam.");
            }
        }

        [HttpGet("user-attempts/{userId}")]
        [ProducesResponseType(typeof(IEnumerable<ExamAttemptResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<ExamAttemptResponseDTO>>> GetAttemptByUserId(int userId)
        {
            try
            {
                if (userId <= 0)
                {
                    return BadRequest(new { Message = "Invalid userId." });
                }

                var attempts = await _examAttemptService.GetAllExamAttempts(userId);
                return Ok(attempts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving exam attempts for UserID {UserID}", userId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while retrieving exam attempts.");
            }
        }

        [HttpGet("attempt-details/{attemptId}")]
        [ProducesResponseType(typeof(ExamAttemptDetailResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ExamAttemptDetailResponseDTO>> GetExamAttemptById(int attemptId)
        {
            try
            {
                if (attemptId <= 0)
                {
                    return BadRequest(new { Message = "Invalid attemptId." });
                }

                var attemptDetails = await _examAttemptService.GetExamAttemptById(attemptId);

                if (attemptDetails == null)
                {
                    _logger.LogWarning("Attempted to retrieve details for non-existent Exam Attempt ID {AttemptID}", attemptId);
                    return NotFound(new { Message = $"Exam attempt with ID {attemptId} not found." });
                }

                return Ok(attemptDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving details for Exam Attempt ID {AttemptID}", attemptId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while retrieving exam attempt details.");
            }
        }


        [HttpPost("save-reading-answer")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SaveReadingAnswer([FromBody] ReadingAnswerRequestDTO model)
        {
            try
            {
                if (model == null)
                {
                    return BadRequest(new { Message = "Request body cannot be null." });
                }
                var result = await _examAttemptService.SaveReadingAnswer(model);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while saving reading answer for AttemptID {AttemptID}", model?.AttemptID);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while saving the reading answer.");
            }
        }

        [HttpPost("save-writing-answer")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SaveWritingAnswer([FromBody] WritingAnswerRequestDTO model)
        {
            try
            {
                if (model == null)
                {
                    return BadRequest(new { Message = "Request body cannot be null." });
                }
                var result = await _examAttemptService.SaveWritingAnswer(model);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while saving writing answer for AttemptID {AttemptID}", model?.AttemptID);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while saving the writing answer.");
            }
        }
    }
}