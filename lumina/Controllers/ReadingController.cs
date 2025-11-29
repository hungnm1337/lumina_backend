using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Exam.Reading;
using DataLayer.DTOs.UserAnswer;
using Microsoft.AspNetCore.Authorization;

namespace lumina.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReadingController : ControllerBase
    {
        private readonly IReadingService _readingService;

        public ReadingController(IReadingService readingService)
        {
            _readingService = readingService;
        }

        [HttpPost("submit-answer")]
        [ProducesResponseType(typeof(SubmitAnswerResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SubmitAnswer([FromBody] ReadingAnswerRequestDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _readingService.SubmitAnswerAsync(request);

                if (!result.Success)
                    return BadRequest(new { message = result.Message });

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { message = "An error occurred while submitting the answer", detail = ex.Message });
            }
        }
    }
}
