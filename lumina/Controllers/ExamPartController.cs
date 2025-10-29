using Microsoft.AspNetCore.Mvc;

namespace lumina.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExamPartController : ControllerBase
    {
        private readonly IExamPartService _examService;

        public ExamPartController(IExamPartService examService)
        {
            _examService = examService;
        }

        [HttpGet("parts")]
        public async Task<IActionResult> GetExamParts()
        {
            try
            {
                var parts = await _examService.GetAllPartsAsync();
                return Ok(parts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Lỗi khi lấy dữ liệu Part: " + ex.Message);
            }
        }


        [HttpGet("exam-parts")]
        public async Task<IActionResult> GetAllExamParts()
        {
            var data = await _examService.GetAllExamPartAsync();
            return Ok(data);
        }

    }

}

