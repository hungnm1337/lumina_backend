using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Exam.Listening;
using DataLayer.DTOs.UserAnswer;

namespace lumina.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ListeningController : ControllerBase
    {
        private readonly IListeningService _listeningService;

        public ListeningController(IListeningService listeningService)
        {
            _listeningService = listeningService;
        }

        [HttpPost("submit-answer")]
        public async Task<IActionResult> SubmitAnswer([FromBody] SubmitAnswerRequestDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _listeningService.SubmitAnswerAsync(request);

                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while submitting the answer.", error = ex.Message });
            }
        }
    }
}
