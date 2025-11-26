using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.MockTest;
using DataLayer.DTOs.MockTest;
using System;
using System.Threading.Tasks;
using DataLayer.DTOs.Exam;

namespace lumina.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MockTestController : ControllerBase
    {
        private readonly IMockTestService _mockTestService;

        public MockTestController(IMockTestService mockTestService)
        {
            _mockTestService = mockTestService;
        }

        [HttpGet("questions")]
        public async Task<ActionResult<List<ExamPartDTO>>> getMocktestInformation()
        {
            try
            {
                var result = await _mockTestService.GetMocktestAsync();
                if (result == null)
                {
                    return NotFound("No mock test information found.");
                }
                return Ok(result);
            }
            catch (Exception ex)
            { 
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("feedback/{examAttemptId}")]
        public async Task<ActionResult<MocktestFeedbackDTO>> GetMocktestFeedback(int examAttemptId)
        {
            try
            {
                if (examAttemptId <= 0)
                {
                    return BadRequest("Invalid exam attempt ID. It must be greater than 0.");
                }

                var result = await _mockTestService.GetMocktestFeedbackAsync(examAttemptId);
                if (result == null)
                {
                    return NotFound($"No feedback found for exam attempt ID: {examAttemptId}");
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
       
    }
}
