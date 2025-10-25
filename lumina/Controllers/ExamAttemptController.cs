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
        [ProducesResponseType(typeof(ExamAttemptDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> StartAnExam([FromBody] ExamAttemptDTO model)
        {
            try
            {
                var result = await _examAttemptService.StartAnExam(model);
         
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while starting exam for UserID {UserID}, ExamID {ExamID}", model.UserID, model.ExamID);

                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while starting the exam.");
            }
        }

        [HttpPatch("end-exam")]
        [ProducesResponseType(typeof(ExamAttemptDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)] 
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> EndAnExam([FromBody] ExamAttemptDTO model)
        {
            try
            {
                var result = await _examAttemptService.EndAnExam(model);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Attempted to end a non-existent exam attempt. ID: {AttemptID}", model.AttemptID);
                return NotFound(new { Message = ex.Message }); 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while ending exam attempt {AttemptID}", model.AttemptID);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while ending the exam.");
            }
        }
    }
}