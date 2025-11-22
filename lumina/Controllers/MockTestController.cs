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
       
    }
}
